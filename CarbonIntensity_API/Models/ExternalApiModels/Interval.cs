using System.Text.Json.Serialization;

namespace CarbonIntensity_API.Models.ExternalApiModels;

public class Interval
{
    [JsonPropertyName("from")]
    public DateTime From { get; set; }
    
    [JsonPropertyName("to")]
    public DateTime To { get; set; }

    [JsonPropertyName("generationmix")]
    public List<GenerationMix> GenerationMixes { get; init; } = new List<GenerationMix>();
}