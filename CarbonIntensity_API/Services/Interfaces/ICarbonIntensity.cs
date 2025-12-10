using CarbonIntensity_API.Models.DTOs;

namespace CarbonIntensity_API.Services.Interfaces;

public interface ICarbonIntensity
{
    Task<EnergyMixResponse> GetAverageEnergyMixAsync();
    // Task<OptimalChargingWindow> GetOptimalWindowAsync(int hours);
}