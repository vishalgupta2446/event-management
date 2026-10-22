using System.Text.Json.Serialization;
using EventManagement.Api.Data;
using EventManagement.Api.Repositories;
using EventManagement.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// In-memory storage must outlive individual requests, so the store and the service
// that guards it are singletons.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IEventRepository, InMemoryEventRepository>();
builder.Services.AddSingleton<IEventService, EventService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Event Management API v1"));
}

if (app.Configuration.GetValue<bool>("SeedSampleData"))
{
    SampleDataSeeder.Seed(
        app.Services.GetRequiredService<IEventRepository>(),
        app.Services.GetRequiredService<TimeProvider>());
}

app.UseCors();
app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
