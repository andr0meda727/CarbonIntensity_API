using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using CarbonIntensity_API.Constants;
using CarbonIntensity_API.Models.DTOs;
using CarbonIntensity_API.Models.ExternalApiModels;
using CarbonIntensity_API.Services.Interfaces;

namespace CarbonIntensity_API.Services;

public class CarbonIntensityService(HttpClient httpClient) : ICarbonIntensity
{
    private const string IsoFormat = "yyyy-MM-ddTHH:mm:ssZ";
    // Return the average energy mix and the percentage of clean energy
    // for 3 days: today, tomorrow, and the day after tomorrow.
    // KEEP IN MIND: Data is available up to 2 days ahead of real-time.
    // Depending on when the data is fetched, we may have estimates
    // for the entire day or only a few hours for the day after tomorrow.
    // e.g. We fetch data at 10.12.2025 16:00, we may have data for 12.12.2025 20:00 - 20:30 interval
    public async Task<EnergyMixResponse> GetAverageEnergyMixAsync()
    {
        var energyMixDays = new List<EnergyMixDay>();
        
        // We need to get data for today and two consecutive days
        // e.g. (10.12.2025, 11.12.2025, 12.12.2025)
        var timeWindow = GetThreeConsecutiveDaysTimeWindow();

        string? rawData = await FetchDataAsync(timeWindow.today, timeWindow.dayAfterTomorrow);
        if (rawData == null) return new EnergyMixResponse { EnergyMixDays = energyMixDays };
        
        var intervals = ProcessData(rawData);
        if (intervals == null || intervals.Count == 0) return new EnergyMixResponse { EnergyMixDays = energyMixDays };
        
        energyMixDays = CalculateAverageAndCleanEnergyPercentage(intervals);
        
        return new EnergyMixResponse { EnergyMixDays = energyMixDays };
    }
    
    private static (string today, string dayAfterTomorrow) GetThreeConsecutiveDaysTimeWindow()
    {
        // Final time window that we need to get data from will be 3 full days (today, tomorrow, day after tomorrow)
        
        // Added 30 minutes, since API was returning 23:30 - 00:00 interval
        var today = DateTime.Now.Date.AddMinutes(30); // Will be 2025-12-10T00:30:00Z, because of the formatting below
        var dayAfterTomorrow = today.AddDays(3); // To 2025-12-13T00:00:00Z
        
        return (today.ToString("yyyy-MM-ddTHH:mm:ssZ"), dayAfterTomorrow.ToString("yyyy-MM-ddTHH:mm:ssZ"));
    }

    private async Task<string?> FetchDataAsync(string from, string to)
    {
        var url = $"https://api.carbonintensity.org.uk/generation/{from}/{to}";
        HttpResponseMessage response;

        try
        {
            response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while fetching data: {ex.Message}");
            return null;
        }

        return await response.Content.ReadAsStringAsync();
    }

    private static List<Interval>? ProcessData(string rawData)
    {
        try
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse>(rawData);
            return apiResponse?.Data;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error deserializing data: {ex.Message}");
            return null;
        }
    }

    private List<EnergyMixDay> CalculateAverageAndCleanEnergyPercentage(List<Interval> intervals)
    {
        var result = new List<EnergyMixDay>();
        
        var groupedIntervalsByDay = intervals
            .GroupBy(interval => interval.From.Date)
            .Select(group => new
            {
                Date = group.Key,
                Intervals = group.ToList()
            })
            .ToList();

        foreach (var day in groupedIntervalsByDay)
        {
            var energyMixDay = CalculateAverageAndCleanEnergyPercentageForSpecificDay(day.Date, day.Intervals);
            result.Add(energyMixDay);
        }
        
        return result;
    }

    private EnergyMixDay CalculateAverageAndCleanEnergyPercentageForSpecificDay(DateTime day, List<Interval> intervals)
    {
        var result = new Dictionary<string, double>();
        var count = intervals.Count;
        var cleanEnergyPercentage = 0.0;
        
        if (count == 0)
        {
            return new EnergyMixDay() { Date = day, AverageEnergyMix = new Dictionary<string, double>(), CleanEnergyPercentage = 0.0 };
        }
        
        foreach (var interval in intervals)
        {
            foreach (var mix in interval.GenerationMixes)
            {
                result.TryAdd(mix.FuelType, 0);

                result[mix.FuelType] += mix.Percentage;
            }
        }

        foreach (var mix in result)
        {
            double average = Math.Round(mix.Value / count, 2);
            result[mix.Key] = average;

            if (EnergyConstants.CleanEnergyTypes.Contains(mix.Key))
            {
                cleanEnergyPercentage += average;
            }
        }
        
        return new EnergyMixDay { Date = day, AverageEnergyMix = result, CleanEnergyPercentage = Math.Round(cleanEnergyPercentage, 2) };
    }
    
    // GetOptimalWindowAsync(int hours)
}