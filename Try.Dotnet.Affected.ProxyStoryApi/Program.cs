var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

var apiUrl = app.Configuration.GetValue<string>("StoryApiUrl");
Console.WriteLine(apiUrl);

var client = new HttpClient()
{
    BaseAddress = new Uri(apiUrl),
};

app.MapGet("/story", async () => $"Proxy: {await client.GetStringAsync("/story")}");

app.Run();
