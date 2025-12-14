using CarbonIntensity_API.Services;
using CarbonIntensity_API.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);
var apiUrl = builder.Configuration["CarbonIntensityApi:BaseUrl"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.WithOrigins(
                    "http://localhost:5173",
                    "https://carbonintensity-frontend.onrender.com"
                )
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

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
app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.MapControllers();

await app.RunAsync();