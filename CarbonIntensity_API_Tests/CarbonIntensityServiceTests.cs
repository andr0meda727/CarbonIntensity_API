using System.Net;
using System.Text.Json;
using CarbonIntensity_API.Models.ExternalApiModels;
using CarbonIntensity_API.Services;
using Moq;
using Moq.Protected;

namespace CarbonIntensity_API_Tests;

[TestFixture]
public class CarbonIntensityServiceTests
{
    private Mock<HttpMessageHandler>? _mockHttpMessageHandler;
    private HttpClient _httpClient;
    private CarbonIntensityService _service;

    [SetUp]
    public void Setup()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.carbonintensity.org.uk/generation/")
        };
        _service = new CarbonIntensityService(_httpClient);
    }
    
    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
        _mockHttpMessageHandler = null;
    }


    [Test]
    public async Task GetAverageEnergyMixAsync_ReturnsData_WhenApiRespondsSuccessfully()
    {
        // Arrange
        var apiResponse = CreateMockApiResponse();
        var jsonResponse = JsonSerializer.Serialize(apiResponse);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _service.GetAverageEnergyMixAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.IsNotEmpty(result.EnergyMixDays);
        Assert.That(result.EnergyMixDays.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetAverageEnergyMixAsync_ReturnsNull_WhenApiReturnsError()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        var result = await _service.GetAverageEnergyMixAsync();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetAverageEnergyMixAsync_ReturnsNull_WhenApiReturnsInvalidJson()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("invalid json")
            });

        // Act
        var result = await _service.GetAverageEnergyMixAsync();

        // Assert
        Assert.IsNull(result);
    }

    [Test]
    public async Task GetOptimalWindowAsync_ReturnsWindow_WhenDataIsAvailable()
    {
        // Arrange
        var apiResponse = CreateMockApiResponseForOptimalWindow(48); // 24 hours of data
        var jsonResponse = JsonSerializer.Serialize(apiResponse);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _service.GetOptimalWindowAsync(3);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StartTime, Is.LessThan(result.EndTime));
        Assert.That(result.CleanEnergyPercentage, Is.GreaterThanOrEqualTo(0));
        Assert.That(result.CleanEnergyPercentage, Is.LessThanOrEqualTo(100));
    }

    [Test]
    public async Task GetOptimalWindowAsync_ReturnsNull_WhenNotEnoughData()
    {
        // Arrange
        var apiResponse = CreateMockApiResponseForOptimalWindow(2); // Only 1 hour of data
        var jsonResponse = JsonSerializer.Serialize(apiResponse);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _service.GetOptimalWindowAsync(3);

        // Assert
        Assert.IsNull(result);
    }

    [Test]
    public async Task GetOptimalWindowAsync_ReturnsNull_WhenApiReturnsError()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        // Act
        var result = await _service.GetOptimalWindowAsync(3);

        // Assert
        Assert.IsNull(result);
    }

    [Test]
    public async Task GetOptimalWindowAsync_FindsWindowWithHighestCleanEnergy()
    {
        // Arrange - create data where clean energy varies
        var apiResponse = new ApiResponse
        {
            Data = new List<Interval>()
        };

        var baseTime = DateTime.UtcNow.Date.AddDays(1);
        
        // First 6 intervals with low clean energy (30%)
        for (int i = 0; i < 6; i++)
        {
            apiResponse.Data.Add(CreateInterval(baseTime.AddMinutes(i * 30), 30, 70));
        }
        
        // Next 6 intervals with high clean energy (80%)
        for (int i = 6; i < 12; i++)
        {
            apiResponse.Data.Add(CreateInterval(baseTime.AddMinutes(i * 30), 80, 20));
        }
        
        // Last 6 intervals with medium clean energy (50%)
        for (int i = 12; i < 18; i++)
        {
            apiResponse.Data.Add(CreateInterval(baseTime.AddMinutes(i * 30), 50, 50));
        }

        var jsonResponse = JsonSerializer.Serialize(apiResponse);

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        // Act
        var result = await _service.GetOptimalWindowAsync(3);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.CleanEnergyPercentage, Is.EqualTo(80));
    }

    private static ApiResponse CreateMockApiResponse()
    {
        var response = new ApiResponse();
        var baseTime = DateTime.UtcNow.Date;

        // Create 3 days of data with 48 intervals per day
        for (int day = 0; day < 3; day++)
        {
            for (int i = 0; i < 48; i++)
            {
                var from = baseTime.AddDays(day).AddMinutes(i * 30);
                var to = from.AddMinutes(30);

                response.Data.Add(new Interval
                {
                    From = from,
                    To = to,
                    GenerationMixes = new List<GenerationMix>
                    {
                        new GenerationMix { FuelType = "wind", Percentage = 25.5 },
                        new GenerationMix { FuelType = "solar", Percentage = 10.2 },
                        new GenerationMix { FuelType = "gas", Percentage = 40.3 },
                        new GenerationMix { FuelType = "nuclear", Percentage = 24.0 }
                    }
                });
            }
        }

        return response;
    }

    private static ApiResponse CreateMockApiResponseForOptimalWindow(int intervalCount)
    {
        var response = new ApiResponse();
        var baseTime = DateTime.UtcNow.Date.AddDays(1);

        for (int i = 0; i < intervalCount; i++)
        {
            var from = baseTime.AddMinutes(i * 30);
            var to = from.AddMinutes(30);

            response.Data.Add(new Interval
            {
                From = from,
                To = to,
                GenerationMixes = new List<GenerationMix>
                {
                    new GenerationMix { FuelType = "wind", Percentage = 30.0 },
                    new GenerationMix { FuelType = "solar", Percentage = 15.0 },
                    new GenerationMix { FuelType = "gas", Percentage = 35.0 },
                    new GenerationMix { FuelType = "nuclear", Percentage = 20.0 }
                }
            });
        }

        return response;
    }

    private static Interval CreateInterval(DateTime from, double cleanPercentage, double dirtyPercentage)
    {
        return new Interval
        {
            From = from,
            To = from.AddMinutes(30),
            GenerationMixes = new List<GenerationMix>
            {
                new GenerationMix { FuelType = "wind", Percentage = cleanPercentage },
                new GenerationMix { FuelType = "gas", Percentage = dirtyPercentage }
            }
        };
    }
}