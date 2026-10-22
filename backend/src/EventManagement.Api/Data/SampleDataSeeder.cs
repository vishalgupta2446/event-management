using EventManagement.Api.Models;
using EventManagement.Api.Repositories;

namespace EventManagement.Api.Data;

/// <summary>
/// Populates the in-memory store with a few demo events so the UI isn't empty on first run.
/// Writes go straight to the repository so a past event can be included for demonstration.
/// </summary>
public static class SampleDataSeeder
{
    public static void Seed(IEventRepository repository, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        var today = new DateTimeOffset(now.Date, TimeSpan.Zero);

        var techTalk = NewEvent(
            "Intro to ASP.NET Core",
            "A hands-on walkthrough of building REST APIs with ASP.NET Core.",
            today.AddDays(7).AddHours(17), 50, now);

        var workshop = NewEvent(
            "React Workshop",
            "Build a single-page application with React, Vite and TanStack Query. Seats are limited!",
            today.AddDays(14).AddHours(10), 3, now);

        var meetup = NewEvent(
            "Community Meetup",
            "Networking, lightning talks and pizza.",
            today.AddDays(30).AddHours(18), 100, now);

        var retro = NewEvent(
            "Year-in-Review Retro",
            "A look back at what we shipped. (Past event — registration is closed.)",
            today.AddDays(-10).AddHours(15), 20, now.AddDays(-30));

        AddAttendee(workshop, "Ada Lovelace", "ada@example.com", now);
        AddAttendee(workshop, "Alan Turing", "alan@example.com", now);
        AddAttendee(techTalk, "Grace Hopper", "grace@example.com", now);
        AddAttendee(retro, "Linus Torvalds", "linus@example.com", now.AddDays(-20));

        foreach (var evt in new[] { techTalk, workshop, meetup, retro })
        {
            repository.Add(evt);
        }
    }

    private static Event NewEvent(string title, string description, DateTimeOffset date, int capacity, DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Date = date,
            MaxCapacity = capacity,
            CreatedAt = createdAt,
        };

    private static void AddAttendee(Event evt, string name, string email, DateTimeOffset at) =>
        evt.AddRegistration(new Registration
        {
            Id = Guid.NewGuid(),
            EventId = evt.Id,
            Name = name,
            Email = email,
            RegisteredAt = at,
        });
}
