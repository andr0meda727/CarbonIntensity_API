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
    // e.g. We fetch data at 10.12.2025 16:00, we may not have data for 12.12.2025 20:00 - 20:30 interval
    public async Task<EnergyMixResponse?> GetAverageEnergyMixAsync()
    {
        // We need to get data for today and two consecutive days
        // e.g. (10.12.2025, 11.12.2025, 12.12.2025)
        var timeWindow = GetThreeConsecutiveDaysTimeWindow();

        string? rawData = await FetchDataAsync(timeWindow.today, timeWindow.dayAfterTomorrow);
        if (rawData == null) return null;
        
        var intervals = ProcessData(rawData);
        if (intervals == null || intervals.Count == 0) return null;
        
        var energyMixDays = CalculateAverageAndCleanEnergyPercentage(intervals);
        
        return new EnergyMixResponse { EnergyMixDays = energyMixDays };
    }

    public async Task<OptimalChargingWindow?> GetOptimalWindowAsync(int hours)
    {
        // We need to get data for two consecutive days
        // e.g. today is 13.12, we need data for 14.12.2025, 15.12.2025
        var timeWindow = GetTimeWindowForOptimalWindow();

        string? rawData = await FetchDataAsync(timeWindow.tommorow, timeWindow.dayAfterTomorrow);
        if (rawData == null) return null;
        
        var intervals = ProcessData(rawData);
        if (intervals == null || intervals.Count == 0) return null;

        var optimalChargingWindow = FindOptimalWindow(intervals, hours);

        return optimalChargingWindow;
    }

    private OptimalChargingWindow? FindOptimalWindow(List<Interval> intervals, int hours)
    {
        var windowSize = 2 * hours; // Intervals are half an hour long
        
        var startTime = intervals[0].From;

        if (intervals.Count < windowSize)
        {
            return null;
        }
        
        var endTime = intervals[windowSize - 1].To;
        
        double windowSum = 0;

        for (int i = 0; i < windowSize; i++)
        {
            windowSum += CalculateCleanEnergyShare(intervals[i]);
        }
        
        double maxSum = windowSum;
        
        // I used sliding window, because of better time complexity
        // O(n) > O(n * windowSize)
        for (int i = windowSize; i < intervals.Count; i++)
        {
            windowSum += CalculateCleanEnergyShare(intervals[i]) - CalculateCleanEnergyShare(intervals[i - windowSize]);

            if (windowSum > maxSum)
            {
                startTime = intervals[i - windowSize + 1].From;
                endTime = intervals[i].To;
                maxSum = windowSum;
            }
        }
        
        return new OptimalChargingWindow{ StartTime = startTime, EndTime = endTime, CleanEnergyPercentage = Math.Round(maxSum / windowSize, 2) };
    }

    private static double CalculateCleanEnergyShare(Interval interval)
    {
        return interval.GenerationMixes.Where((mix) => EnergyConstants.CleanEnergyTypes.Contains(mix.FuelType)).Sum((mix) => mix.Percentage);
    }

    private static (string today, string dayAfterTomorrow) GetThreeConsecutiveDaysTimeWindow()
    {
        // Final time window that we need to get data from will be 3 full days (today, tomorrow, day after tomorrow)
        
        // Added 1 minute, since API was returning 23:30 - 00:00 interval (yesterday)
        var today = DateTime.UtcNow.Date.AddMinutes(1); // From 2025-12-10T00:01:00Z
        var dayAfterTomorrow = today.AddDays(3);        // To 2025-12-13T00:01:00Z
        
        return (today.ToString(IsoFormat), dayAfterTomorrow.ToString(IsoFormat));
    }

    private static (string tommorow, string dayAfterTomorrow) GetTimeWindowForOptimalWindow()
    {
        // Added 1 minute, since API was returning 23:30 - 00:00 interval (yesterday)
        var tomorrow = DateTime.UtcNow.Date.AddMinutes(1).AddDays(1);   // From 2025-12-11T00:01:00Z
        var dayAfterTomorrow = tomorrow.AddDays(2);                     // To 2025-12-13T01:00:00Z
        
        return (tomorrow.ToString(IsoFormat), dayAfterTomorrow.ToString(IsoFormat));
    }

    private async Task<string?> FetchDataAsync(string from, string to)
    {
        var url = $"{from}/{to}";
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
        var groupedIntervalsByDay = intervals.GroupBy(interval => interval.From.Date)
            .Select(group => new { Date = group.Key, Intervals = group.ToList() })
            .ToList();

        var result = groupedIntervalsByDay
            .Select(day => CalculateAverageAndCleanEnergyPercentageForSpecificDay(day.Date, day.Intervals))
            .ToList();

        return result;
    }

    private static EnergyMixDay CalculateAverageAndCleanEnergyPercentageForSpecificDay(DateTime day, List<Interval> intervals)
    {
        var result = new Dictionary<string, double>();
        var count = intervals.Count;
        var cleanEnergyPercentage = 0.0;
        
        if (count == 0)
            return new EnergyMixDay() { Date = day, AverageEnergyMix = new Dictionary<string, double>(), CleanEnergyPercentage = 0.0 };
        
        foreach (var mix in intervals.SelectMany(interval => interval.GenerationMixes))
        {
            result.TryAdd(mix.FuelType, 0);
            result[mix.FuelType] += mix.Percentage;
        }

        foreach (var key in result.Keys.ToList())
        {
            var average = Math.Round(result[key] / count, 2);
            result[key] = average;

            if (EnergyConstants.CleanEnergyTypes.Contains(key)) cleanEnergyPercentage += average;
        }
        
        return new EnergyMixDay { Date = day, AverageEnergyMix = result, CleanEnergyPercentage = Math.Round(cleanEnergyPercentage, 2) };
    }
}