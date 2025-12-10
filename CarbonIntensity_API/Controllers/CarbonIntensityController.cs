using CarbonIntensity_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CarbonIntensity_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CarbonIntensityController : ControllerBase
{
    private readonly ICarbonIntensity _carbonIntensityService;

    public CarbonIntensityController(ICarbonIntensity carbonIntensityService)
    {
        _carbonIntensityService = carbonIntensityService;
    }

    // GetAverageEnergyMix method (today, tommorow, the day after tommorow)
    [HttpGet("energy-mix")]
    public async Task<IActionResult> GetAverageEnergyMix()
    {
        try
        {
            var result = await _carbonIntensityService.GetAverageEnergyMixAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex);
        }
    }
    
    
    // [HttpGet("optimal-window")]
    // GetOptimalWindow method
}