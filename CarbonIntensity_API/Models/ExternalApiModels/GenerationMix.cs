using System.Text.Json.Serialization;

namespace CarbonIntensity_API.Models.ExternalApiModels;

public class GenerationMix
{
    [JsonPropertyName(("fuel"))]
    public required string FuelType { get; set; }
    
    [JsonPropertyName(("perc"))]
    public required double Percentage { get; set; }
}