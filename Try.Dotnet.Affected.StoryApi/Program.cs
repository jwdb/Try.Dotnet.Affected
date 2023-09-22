var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

var summaries = new[]
{
    "a cat", "a dog", "a wild ferret", "the moon", "martians", "kudos", "electrons", "an android", "a gray sky", "rain"
};

app.MapGet("/story", () => $"Once upon a time there was a story about {summaries[Random.Shared.Next(0, summaries.Length)]}");

app.Run();
