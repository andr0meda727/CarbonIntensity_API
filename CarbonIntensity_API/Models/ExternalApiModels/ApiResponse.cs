using System.Text.Json.Serialization;

namespace CarbonIntensity_API.Models.ExternalApiModels;

public class ApiResponse
{
    [JsonPropertyName("data")]
    public List<Interval> Data { get; init; } = new();
}