using CarbonIntensity_API.Services;
using CarbonIntensity_API.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient<ICarbonIntensity, CarbonIntensityService>();

var app = builder.Build();

app.Run();
