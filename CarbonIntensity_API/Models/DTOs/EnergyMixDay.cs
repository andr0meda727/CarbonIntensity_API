namespace CarbonIntensity_API.Models.DTOs;

public class EnergyMixDay
{
    public DateTime Date { get; set; }
    public Dictionary<string, double> AverageEnergyMix { get; set; } = new();
    public double CleanEnergyPercentage { get; set; }
}