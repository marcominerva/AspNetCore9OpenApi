using System.ComponentModel;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "My new API";
        document.Info.Contact = new()
        {
            Name = "Support",
            Email = "support@email.com"
        };

        document.Info.License = new()
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        };

        document.Servers.Clear();

        return Task.CompletedTask;
    });

    options.AddOperationTransformer<AcceptLanguageHeaderOperationTransfomer>();
});

var cultures = new[] { "en", "it" };
var supportedCultures = cultures.Select(c => new CultureInfo(c)).ToList();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.DefaultRequestCulture = new RequestCulture(supportedCultures.First());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.MapOpenApi();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", app.Environment.ApplicationName);
});

//app.MapScalarApiReference();

app.UseRequestLocalization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", ([Description("The number of days")] int days = 5) =>
{
    var forecast = Enumerable.Range(1, days).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithSummary("Get the weather forecast")
.WithDescription("The forecast for the next days");

app.MapPost("/api/person", ([Description("The person to create")] Person person) =>
{
    return TypedResults.Ok(person);
});

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class Person
{
    [Description("The person name")]
    public string? Name { get; set; }

    [Description("The city where the person lives")]
    [DefaultValue("Taggia")]
    public string? City { get; set; }
}

public class AcceptLanguageHeaderOperationTransfomer(IOptions<RequestLocalizationOptions> requestLocalizationOptions) : IOpenApiOperationTransformer
{
    private readonly List<IOpenApiAny>? supportedLanguages = requestLocalizationOptions.Value
        .SupportedCultures?.Select(c => new OpenApiString(c.Name))
        .Cast<IOpenApiAny>()
        .ToList();

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (supportedLanguages?.Count > 0)
        {
            operation.Parameters ??= [];

            if (!operation.Parameters.Any(p => p.Name == HeaderNames.AcceptLanguage && p.In == ParameterLocation.Header))
            {
                operation.Parameters.Add(new()
                {
                    Name = HeaderNames.AcceptLanguage,
                    In = ParameterLocation.Header,
                    Required = false,
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Enum = supportedLanguages,
                        Default = supportedLanguages.First()
                    }
                });
            }
        }

        return Task.CompletedTask;
    }
}