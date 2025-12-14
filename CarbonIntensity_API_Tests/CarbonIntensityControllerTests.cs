using CarbonIntensity_API.Controllers;
using CarbonIntensity_API.Models.DTOs;
using CarbonIntensity_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarbonIntensity_API_Tests;

[TestFixture]
public class CarbonIntensityControllerTests
{
    private Mock<ICarbonIntensity> _mockService;
    private CarbonIntensityController _controller;

    [SetUp]
    public void Setup()
    {
        _mockService = new Mock<ICarbonIntensity>();
        _controller = new CarbonIntensityController(_mockService.Object);
    }

    [Test]
    public async Task GetAverageEnergyMix_ReturnsOkResult_WhenServiceReturnsData()
    {
        // Arrange
        var expectedResponse = new EnergyMixResponse
        {
            EnergyMixDays = new List<EnergyMixDay>
            {
                new EnergyMixDay
                {
                    Date = DateTime.UtcNow.Date,
                    AverageEnergyMix = new Dictionary<string, double> { { "wind", 25.5 } },
                    CleanEnergyPercentage = 45.2
                }
            }
        };
        _mockService.Setup(s => s.GetAverageEnergyMixAsync()).ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetAverageEnergyMix();

        // Assert
        Assert.That(result, Is.InstanceOf<ActionResult<EnergyMixResponse>>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
        Assert.That(okResult.Value, Is.EqualTo(expectedResponse));
    }

    [Test]
    public async Task GetAverageEnergyMix_Returns500_WhenServiceReturnsNull()
    {
        // Arrange
        _mockService.Setup(s => s.GetAverageEnergyMixAsync()).ReturnsAsync((EnergyMixResponse?)null);

        // Act
        var result = await _controller.GetAverageEnergyMix();

        // Assert
        var statusCodeResult = result.Result as ObjectResult;
        Assert.That(statusCodeResult, Is.Not.Null);
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task GetAverageEnergyMix_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        _mockService.Setup(s => s.GetAverageEnergyMixAsync()).ThrowsAsync(new Exception("API Error"));

        // Act
        var result = await _controller.GetAverageEnergyMix();

        // Assert
        var statusCodeResult = result.Result as ObjectResult;
        Assert.That(statusCodeResult, Is.Not.Null);
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task GetOptimalWindow_ReturnsOkResult_WhenHoursAreValid()
    {
        // Arrange
        var expectedWindow = new OptimalChargingWindow
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(3),
            CleanEnergyPercentage = 55.8
        };
        _mockService.Setup(s => s.GetOptimalWindowAsync(3)).ReturnsAsync(expectedWindow);

        // Act
        var result = await _controller.GetOptimalWindow(3);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
        Assert.That(okResult.Value, Is.EqualTo(expectedWindow));
    }

    [Test]
    [TestCase(0)]
    [TestCase(7)]
    [TestCase(-1)]
    public async Task GetOptimalWindow_ReturnsBadRequest_WhenHoursAreOutOfRange(int hours)
    {
        // Act
        var result = await _controller.GetOptimalWindow(hours);

        // Assert
        var badRequestResult = result.Result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult.StatusCode, Is.EqualTo(400));
    }

    [Test]
    [TestCase(1)]
    [TestCase(6)]
    public async Task GetOptimalWindow_AcceptsValidHours(int hours)
    {
        // Arrange
        var expectedWindow = new OptimalChargingWindow
        {
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(hours),
            CleanEnergyPercentage = 50.0
        };
        _mockService.Setup(s => s.GetOptimalWindowAsync(hours)).ReturnsAsync(expectedWindow);

        // Act
        var result = await _controller.GetOptimalWindow(hours);

        // Assert
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task GetOptimalWindow_Returns500_WhenServiceReturnsNull()
    {
        // Arrange
        _mockService.Setup(s => s.GetOptimalWindowAsync(3)).ReturnsAsync((OptimalChargingWindow?)null);

        // Act
        var result = await _controller.GetOptimalWindow(3);

        // Assert
        var statusCodeResult = result.Result as ObjectResult;
        Assert.That(statusCodeResult, Is.Not.Null);
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task GetOptimalWindow_Returns500_WhenServiceThrowsException()
    {
        // Arrange
        _mockService.Setup(s => s.GetOptimalWindowAsync(3)).ThrowsAsync(new Exception("Calculation Error"));

        // Act
        var result = await _controller.GetOptimalWindow(3);

        // Assert
        var statusCodeResult = result.Result as ObjectResult;
        Assert.That(statusCodeResult, Is.Not.Null);
        Assert.That(statusCodeResult.StatusCode, Is.EqualTo(500));
    }
}