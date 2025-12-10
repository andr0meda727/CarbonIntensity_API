namespace CarbonIntensity_API.Models.DTOs;

public class OptimalChargingWindow
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double CleanEnergyPercentage { get; set; }
}