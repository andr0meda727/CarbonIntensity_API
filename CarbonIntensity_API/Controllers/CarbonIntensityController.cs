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
            return Ok(result);
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
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex);
        }
    }
}