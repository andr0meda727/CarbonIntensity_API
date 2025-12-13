using CarbonIntensity_API.Services;
using CarbonIntensity_API.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);
var apiUrl = builder.Configuration["CarbonIntensityApi:BaseUrl"];

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<ICarbonIntensity, CarbonIntensityService>(client =>
{
    if (string.IsNullOrWhiteSpace(apiUrl)) 
    {
        throw new InvalidOperationException("CarbonIntensityApi:BaseUrl is missing in appsettings.json");
    }
    
    client.BaseAddress = new Uri(apiUrl);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

await app.RunAsync();