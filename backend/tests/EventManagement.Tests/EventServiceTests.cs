using EventManagement.Api.Common;
using EventManagement.Api.Contracts;
using EventManagement.Api.Repositories;
using EventManagement.Api.Services;
using Microsoft.Extensions.Time.Testing;

namespace EventManagement.Tests;

public sealed class EventServiceTests
{
    private static readonly DateTimeOffset Start = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(Start);
    private readonly EventService _sut;

    public EventServiceTests() => _sut = new EventService(new InMemoryEventRepository(), _clock);

    // ---------- helpers ----------

    private static EventRequest NewEventRequest(
        string title = "Conference",
        DateTimeOffset? date = null,
        int capacity = 10,
        string? description = "Desc") =>
        new() { Title = title, Description = description, Date = date ?? Start.AddDays(7), MaxCapacity = capacity };

    private static RegistrationRequest Attendee(string email, string name = "Attendee") =>
        new() { Name = name, Email = email };

    private EventResponse CreateEvent(int capacity = 10, DateTimeOffset? date = null) =>
        _sut.CreateEvent(NewEventRequest(capacity: capacity, date: date)).Value;

    private static void AssertError(Result result, Error expected)
    {
        Assert.False(result.IsSuccess);
        Assert.Equal(expected.Type, result.Error!.Type);
        Assert.Equal(expected.Code, result.Error.Code);
    }

    // ---------- create ----------

    [Fact]
    public void CreateEvent_WithValidData_ReturnsEventWithFullCapacity()
    {
        var result = _sut.CreateEvent(NewEventRequest(title: "  Conference  ", capacity: 25, description: "   "));

        Assert.True(result.IsSuccess);
        var evt = result.Value;
        Assert.NotEqual(Guid.Empty, evt.Id);
        Assert.Equal("Conference", evt.Title);
        Assert.Null(evt.Description);
        Assert.Equal(25, evt.MaxCapacity);
        Assert.Equal(0, evt.RegisteredCount);
        Assert.Equal(25, evt.RemainingCapacity);
        Assert.False(evt.IsFull);
        Assert.False(evt.IsPast);
        Assert.Equal(Start, evt.CreatedAt);
    }

    [Theory]
    [InlineData(0)]     // now is not "in the future"
    [InlineData(-1)]
    [InlineData(-24 * 365)]
    public void CreateEvent_WithDateNotInFuture_ReturnsValidationError(int hoursFromNow)
    {
        var result = _sut.CreateEvent(NewEventRequest(date: Start.AddHours(hoursFromNow)));

        AssertError(result, EventErrors.DateInPast);
    }

    [Fact]
    public void CreateEvent_WithWhitespaceTitle_ReturnsValidationError()
    {
        var result = _sut.CreateEvent(NewEventRequest(title: "   "));

        AssertError(result, EventErrors.TitleRequired);
    }

    // ---------- read ----------

    [Fact]
    public void GetEvent_WhenMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();

        AssertError(_sut.GetEvent(id), EventErrors.EventNotFound(id));
    }

    [Fact]
    public void GetEvents_FiltersByStatusAndOrdersByDate()
    {
        var later = CreateEvent(date: Start.AddDays(10));
        var soon = CreateEvent(date: Start.AddDays(1));
        var soonest = CreateEvent(date: Start.AddHours(1));

        _clock.Advance(TimeSpan.FromDays(2)); // "soonest" and "soon" are now in the past

        Assert.Equal([soonest.Id, soon.Id, later.Id], _sut.GetEvents().Select(e => e.Id));
        Assert.Equal([later.Id], _sut.GetEvents(EventStatus.Upcoming).Select(e => e.Id));
        Assert.Equal([soon.Id, soonest.Id], _sut.GetEvents(EventStatus.Past).Select(e => e.Id));
        Assert.All(_sut.GetEvents(EventStatus.Past), e => Assert.True(e.IsPast));
    }

    // ---------- update ----------

    [Fact]
    public void UpdateEvent_WithValidData_ReplacesFields()
    {
        var evt = CreateEvent();
        var newDate = Start.AddDays(30);

        var result = _sut.UpdateEvent(evt.Id, NewEventRequest(title: "Renamed", date: newDate, capacity: 99, description: "New"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Renamed", result.Value.Title);
        Assert.Equal("New", result.Value.Description);
        Assert.Equal(newDate, result.Value.Date);
        Assert.Equal(99, result.Value.MaxCapacity);
        Assert.Equal(result.Value, _sut.GetEvent(evt.Id).Value);
    }

    [Fact]
    public void UpdateEvent_WhenMissing_ReturnsNotFound()
    {
        var id = Guid.NewGuid();

        AssertError(_sut.UpdateEvent(id, NewEventRequest()), EventErrors.EventNotFound(id));
    }

    [Fact]
    public void UpdateEvent_ToPastDate_ReturnsValidationError()
    {
        var evt = CreateEvent();

        AssertError(_sut.UpdateEvent(evt.Id, NewEventRequest(date: Start.AddDays(-1))), EventErrors.DateInPast);
    }

    [Fact]
    public void UpdateEvent_WhenEventAlreadyHappened_ReturnsConflict()
    {
        var evt = CreateEvent(date: Start.AddHours(1));
        _clock.Advance(TimeSpan.FromHours(2));

        var result = _sut.UpdateEvent(evt.Id, NewEventRequest(date: Start.AddDays(10)));

        AssertError(result, EventErrors.PastEventLocked);
    }

    [Fact]
    public void UpdateEvent_CapacityBelowRegistrationCount_ReturnsConflict()
    {
        var evt = CreateEvent(capacity: 5);
        _sut.Register(evt.Id, Attendee("a@x.com"));
        _sut.Register(evt.Id, Attendee("b@x.com"));
        _sut.Register(evt.Id, Attendee("c@x.com"));

        var result = _sut.UpdateEvent(evt.Id, NewEventRequest(capacity: 2));

        AssertError(result, EventErrors.CapacityBelowRegistrations(3));
        Assert.Equal(5, _sut.GetEvent(evt.Id).Value.MaxCapacity);
    }

    [Fact]
    public void UpdateEvent_CapacityEqualToRegistrationCount_Succeeds()
    {
        var evt = CreateEvent(capacity: 5);
        _sut.Register(evt.Id, Attendee("a@x.com"));
        _sut.Register(evt.Id, Attendee("b@x.com"));

        var result = _sut.UpdateEvent(evt.Id, NewEventRequest(capacity: 2));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsFull);
    }

    // ---------- delete ----------

    [Fact]
    public void DeleteEvent_RemovesEvent()
    {
        var evt = CreateEvent();

        Assert.True(_sut.DeleteEvent(evt.Id).IsSuccess);
        AssertError(_sut.GetEvent(evt.Id), EventErrors.EventNotFound(evt.Id));
        AssertError(_sut.DeleteEvent(evt.Id), EventErrors.EventNotFound(evt.Id));
    }

    // ---------- register ----------

    [Fact]
    public void Register_WithAvailableCapacity_AddsRegistration()
    {
        var evt = CreateEvent(capacity: 2);

        var result = _sut.Register(evt.Id, Attendee(" ada@example.com ", " Ada "));

        Assert.True(result.IsSuccess);
        Assert.Equal("Ada", result.Value.Name);
        Assert.Equal("ada@example.com", result.Value.Email);
        Assert.Equal(evt.Id, result.Value.EventId);
        Assert.Equal(Start, result.Value.RegisteredAt);

        var updated = _sut.GetEvent(evt.Id).Value;
        Assert.Equal(1, updated.RegisteredCount);
        Assert.Equal(1, updated.RemainingCapacity);
        Assert.Single(_sut.GetRegistrations(evt.Id).Value);
    }

    [Fact]
    public void Register_ForUnknownEvent_ReturnsNotFound()
    {
        var id = Guid.NewGuid();

        AssertError(_sut.Register(id, Attendee("a@x.com")), EventErrors.EventNotFound(id));
    }

    [Fact]
    public void Register_ForPastEvent_ReturnsConflict()
    {
        var evt = CreateEvent(date: Start.AddMinutes(30));
        _clock.Advance(TimeSpan.FromMinutes(30)); // event starts exactly now

        var result = _sut.Register(evt.Id, Attendee("a@x.com"));

        AssertError(result, EventErrors.RegistrationClosed);
        Assert.Empty(_sut.GetRegistrations(evt.Id).Value);
    }

    [Fact]
    public void Register_WhenEventIsFull_ReturnsConflict()
    {
        var evt = CreateEvent(capacity: 2);
        Assert.True(_sut.Register(evt.Id, Attendee("a@x.com")).IsSuccess);
        Assert.True(_sut.Register(evt.Id, Attendee("b@x.com")).IsSuccess);

        var result = _sut.Register(evt.Id, Attendee("c@x.com"));

        AssertError(result, EventErrors.EventFull);
        Assert.Equal(2, _sut.GetEvent(evt.Id).Value.RegisteredCount);
        Assert.True(_sut.GetEvent(evt.Id).Value.IsFull);
    }

    [Theory]
    [InlineData("ada@example.com")]
    [InlineData("ADA@Example.com")]
    [InlineData("  ada@example.com  ")]
    public void Register_SameEmailTwice_ReturnsConflict(string secondEmail)
    {
        var evt = CreateEvent();
        _sut.Register(evt.Id, Attendee("ada@example.com"));

        var result = _sut.Register(evt.Id, Attendee(secondEmail));

        AssertError(result, EventErrors.AlreadyRegistered);
        Assert.Single(_sut.GetRegistrations(evt.Id).Value);
    }

    [Fact]
    public void Register_SameEmailForDifferentEvents_Succeeds()
    {
        var first = CreateEvent();
        var second = CreateEvent();

        Assert.True(_sut.Register(first.Id, Attendee("ada@example.com")).IsSuccess);
        Assert.True(_sut.Register(second.Id, Attendee("ada@example.com")).IsSuccess);
    }

    [Fact]
    public void Register_ConcurrentRequests_NeverExceedCapacity()
    {
        const int capacity = 10;
        var evt = CreateEvent(capacity: capacity);

        var outcomes = new Result[200];
        Parallel.For(0, outcomes.Length, i => outcomes[i] = _sut.Register(evt.Id, Attendee($"user{i}@x.com")));

        Assert.Equal(capacity, outcomes.Count(r => r.IsSuccess));
        Assert.All(outcomes.Where(r => !r.IsSuccess), r => Assert.Equal(EventErrors.EventFull.Code, r.Error!.Code));
        Assert.Equal(capacity, _sut.GetRegistrations(evt.Id).Value.Count);
    }

    // ---------- unregister ----------

    [Fact]
    public void Unregister_RemovesRegistrationAndFreesSeat()
    {
        var evt = CreateEvent(capacity: 1);
        var registration = _sut.Register(evt.Id, Attendee("a@x.com")).Value;

        Assert.True(_sut.Unregister(evt.Id, registration.Id).IsSuccess);

        Assert.Empty(_sut.GetRegistrations(evt.Id).Value);
        Assert.True(_sut.Register(evt.Id, Attendee("b@x.com")).IsSuccess);
    }

    [Fact]
    public void Unregister_ThenRegisterAgain_WithSameEmail_Succeeds()
    {
        var evt = CreateEvent();
        var registration = _sut.Register(evt.Id, Attendee("a@x.com")).Value;
        _sut.Unregister(evt.Id, registration.Id);

        Assert.True(_sut.Register(evt.Id, Attendee("a@x.com")).IsSuccess);
    }

    [Fact]
    public void Unregister_UnknownRegistration_ReturnsNotFound()
    {
        var evt = CreateEvent();
        var id = Guid.NewGuid();

        AssertError(_sut.Unregister(evt.Id, id), EventErrors.RegistrationNotFound(id));
    }

    [Fact]
    public void Unregister_FromPastEvent_ReturnsConflict()
    {
        var evt = CreateEvent(date: Start.AddHours(1));
        var registration = _sut.Register(evt.Id, Attendee("a@x.com")).Value;
        _clock.Advance(TimeSpan.FromHours(2));

        AssertError(_sut.Unregister(evt.Id, registration.Id), EventErrors.UnregistrationClosed);
        Assert.Single(_sut.GetRegistrations(evt.Id).Value);
    }

    [Fact]
    public void GetRegistrations_ForUnknownEvent_ReturnsNotFound()
    {
        var id = Guid.NewGuid();

        AssertError(_sut.GetRegistrations(id), EventErrors.EventNotFound(id));
    }
}
