using CarbonIntensity_API.Models.DTOs;
using CarbonIntensity_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CarbonIntensity_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarbonIntensityController(ICarbonIntensity carbonIntensityService) : ControllerBase
{
    // GetAverageEnergyMix method (today, tomorrow, the day after tommorow)
    [HttpGet("energy-mix")]
    public async Task<ActionResult<EnergyMixResponse>> GetAverageEnergyMix()
    {
        try
        {
            var result = await carbonIntensityService.GetAverageEnergyMixAsync();
            return result == null
                ? StatusCode(500, new { error = "External API unavailable or returned invalid data" })
                : Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex);
        }
    }
    
    
    [HttpGet("optimal-window")]
    public async Task<ActionResult<OptimalChargingWindow>> GetOptimalWindow([FromQuery] int hours)
    {
        if (hours < 1 || hours > 6)
        {
            return BadRequest(new { message = "Hours must be between 1 and 6" });
        }
        
        try
        {
            var result = await carbonIntensityService.GetOptimalWindowAsync(hours);
            return result == null
                ? StatusCode(500, new { error = "Failed to calculate optimal charging window" })
                : Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex);
        }
    }
}