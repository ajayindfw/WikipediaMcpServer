using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using WikipediaMcpServer.Services;
using WikipediaMcpServer.Models;

namespace WikipediaMcpServer.ServiceTests.Services;

public class WikipediaServiceTests : IDisposable
{
    private readonly Mock<ILogger<WikipediaService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly WikipediaService _wikipediaService;

    public WikipediaServiceTests()
    {
        _mockLogger = new Mock<ILogger<WikipediaService>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _wikipediaService = new WikipediaService(_httpClient, _mockLogger.Object);
    }

    [Fact]
    public async Task SearchAsync_WithValidQuery_ShouldReturnResult()
    {
        // Arrange
        const string query = "python programming";
        const string mockResponse = """
            {
                "title": "Python (programming language)",
                "extract": "Python is a high-level programming language.",
                "content_urls": {
                    "desktop": {
                        "page": "https://en.wikipedia.org/wiki/Python_(programming_language)"
                    }
                }
            }
            """;

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _wikipediaService.SearchAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Python (programming language)");
        result.Summary.Should().Be("Python is a high-level programming language.");
        result.Url.Should().Be("https://en.wikipedia.org/wiki/Python_(programming_language)");
    }

    [Fact]
    public async Task SearchAsync_WithHttpError_ShouldReturnNull()
    {
        // Arrange
        const string query = "nonexistent topic";

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var result = await _wikipediaService.SearchAsync(query);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSectionsAsync_WithValidTopic_ShouldReturnSections()
    {
        // Arrange
        const string topic = "Machine learning";
        const string mockResponse = """
            {
                "parse": {
                    "title": "Machine learning",
                    "sections": [
                        {
                            "toclevel": 1,
                            "level": "2",
                            "line": "History",
                            "number": "1",
                            "index": "1",
                            "fromtitle": "Machine_learning",
                            "byteoffset": 1234,
                            "anchor": "History"
                        },
                        {
                            "toclevel": 1,
                            "level": "2",
                            "line": "Overview",
                            "number": "2",
                            "index": "2",
                            "fromtitle": "Machine_learning",
                            "byteoffset": 5678,
                            "anchor": "Overview"
                        }
                    ]
                }
            }
            """;

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _wikipediaService.GetSectionsAsync(topic);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Machine learning");
        result.Sections.Should().NotBeEmpty();
        result.Sections.Should().Contain("History");
        result.Sections.Should().Contain("Overview");
        result.Url.Should().StartWith("https://en.wikipedia.org/wiki/");
    }

    [Fact]
    public async Task GetSectionsAsync_WithHttpError_ShouldReturnNull()
    {
        // Arrange
        const string topic = "nonexistent topic";

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var result = await _wikipediaService.GetSectionsAsync(topic);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSectionContentAsync_WithValidParameters_ShouldReturnContent()
    {
        // Arrange
        const string topic = "Python programming";
        const string sectionTitle = "History";
        
        // Mock sections response first
        const string sectionsResponse = """
            {
                "parse": {
                    "title": "Python (programming language)",
                    "sections": [
                        {
                            "toclevel": 1,
                            "level": "2",
                            "line": "History",
                            "number": "1",
                            "index": "1",
                            "anchor": "History"
                        }
                    ]
                }
            }
            """;

        // Mock content response  
        const string contentResponse = """
            {
                "parse": {
                    "text": {
                        "*": "<div><p>Python was conceived in the late 1980s by Guido van Rossum.</p></div>"
                    }
                }
            }
            """;

        var responseSequence = new Queue<HttpResponseMessage>();
        responseSequence.Enqueue(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(sectionsResponse, Encoding.UTF8, "application/json")
        });
        responseSequence.Enqueue(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(contentResponse, Encoding.UTF8, "application/json")
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => responseSequence.Dequeue());

        // Act
        var result = await _wikipediaService.GetSectionContentAsync(topic, sectionTitle);

        // Assert
        result.Should().NotBeNull();
        result!.SectionTitle.Should().Be(sectionTitle);
        result.Content.Should().Contain("Python was conceived in the late 1980s");
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // PRIORITY 3: Additional comprehensive tests for missing coverage and edge cases

    [Fact]
    public async Task SearchAsync_WithEmptyResponse_ShouldReturnGracefulResult()
    {
        // Arrange
        const string query = "test query";
        const string emptyResponse = "{}";

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(emptyResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _wikipediaService.SearchAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.Summary.Should().Be("No summary available.");
        result.Title.Should().Be("");
        result.Url.Should().Be("");
    }

    [Fact]
    public async Task SearchAsync_WithMalformedJson_ShouldReturnNull()
    {
        // Arrange
        const string query = "test query";
        const string malformedJson = "{ invalid json }";

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(malformedJson, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _wikipediaService.SearchAsync(query);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_WithMissingFields_ShouldReturnGracefulResult()
    {
        // Arrange
        const string query = "test query";
        const string incompleteResponse = """
            {
                "title": "Test Article"
            }
            """;

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(incompleteResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _wikipediaService.SearchAsync(query);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Article");
        result.Summary.Should().Be("No summary available.");
        result.Url.Should().Be("");
    }

    [Fact]
    public async Task SearchAsync_WithHttpException_ShouldReturnNull()
    {
        // Arrange
        const string query = "test query";

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _wikipediaService.SearchAsync(query);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_WithTaskCancellation_ShouldReturnNull()
    {
        // Arrange
        const string query = "test query";

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        // Act
        var result = await _wikipediaService.SearchAsync(query);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSectionsAsync_WithEmptyResponse_ShouldReturnNull()
    {
        // Arrange
        const string topic = "test topic";
        const string emptyResponse = "{}";

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(emptyResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _wikipediaService.GetSectionsAsync(topic);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSectionsAsync_WithMissingParseField_ShouldReturnNull()
    {
        // Arrange
        const string topic = "test topic";
        const string responseWithoutParse = """
            {
                "title": "Test Article"
            }
            """;

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseWithoutParse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _wikipediaService.GetSectionsAsync(topic);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSectionsAsync_WithEmptySections_ShouldReturnGracefulResult()
    {
        // Arrange
        const string topic = "test topic";
        const string responseWithEmptySections = """
            {
                "parse": {
                    "title": "Test Article",
                    "sections": []
                }
            }
            """;

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseWithEmptySections, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _wikipediaService.GetSectionsAsync(topic);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Article");
        result.Sections.Should().HaveCount(1);
        result.Sections.Should().Contain("No sections available");
    }

    [Fact]
    public async Task GetSectionsAsync_WithHttpException_ShouldReturnNull()
    {
        // Arrange
        const string topic = "test topic";

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("API error"));

        // Act
        var result = await _wikipediaService.GetSectionsAsync(topic);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSectionContentAsync_WithSectionNotFound_ShouldReturnHelpfulMessage()
    {
        // Arrange
        const string topic = "Python programming";
        const string sectionTitle = "NonExistentSection";
        
        // Mock sections response without the requested section
        const string sectionsResponse = """
            {
                "parse": {
                    "title": "Python (programming language)",
                    "sections": [
                        {
                            "toclevel": 1,
                            "level": "2",
                            "line": "History",
                            "number": "1",
                            "index": "1",
                            "anchor": "History"
                        }
                    ]
                }
            }
            """;

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(sectionsResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _wikipediaService.GetSectionContentAsync(topic, sectionTitle);

        // Assert
        result.Should().NotBeNull();
        result!.SectionTitle.Should().Be(sectionTitle);
        result.Content.Should().Contain("Section 'NonExistentSection' not found");
        result.Content.Should().Contain("Available sections: History");
    }

    [Fact]
    public async Task GetSectionContentAsync_WithGetSectionsFailure_ShouldReturnNull()
    {
        // Arrange
        const string topic = "nonexistent topic";
        const string sectionTitle = "History";

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var result = await _wikipediaService.GetSectionContentAsync(topic, sectionTitle);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSectionContentAsync_WithContentFetchFailure_ShouldReturnNull()
    {
        // Arrange
        const string topic = "Python programming";
        const string sectionTitle = "History";
        
        // Mock sections response first (success)
        const string sectionsResponse = """
            {
                "parse": {
                    "title": "Python (programming language)",
                    "sections": [
                        {
                            "toclevel": 1,
                            "level": "2",
                            "line": "History",
                            "number": "1",
                            "index": "1",
                            "anchor": "History"
                        }
                    ]
                }
            }
            """;

        var responseSequence = new Queue<HttpResponseMessage>();
        responseSequence.Enqueue(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(sectionsResponse, Encoding.UTF8, "application/json")
        });
        responseSequence.Enqueue(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.InternalServerError // Content fetch failure
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => responseSequence.Dequeue());

        // Act
        var result = await _wikipediaService.GetSectionContentAsync(topic, sectionTitle);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSectionContentAsync_WithEmptyContentResponse_ShouldReturnGracefulMessage()
    {
        // Arrange
        const string topic = "Python programming";
        const string sectionTitle = "History";
        
        // Mock sections response first
        const string sectionsResponse = """
            {
                "parse": {
                    "title": "Python (programming language)",
                    "sections": [
                        {
                            "toclevel": 1,
                            "level": "2",
                            "line": "History",
                            "number": "1",
                            "index": "1",
                            "anchor": "History"
                        }
                    ]
                }
            }
            """;

        // Mock empty content response  
        const string emptyContentResponse = "{}";

        var responseSequence = new Queue<HttpResponseMessage>();
        responseSequence.Enqueue(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(sectionsResponse, Encoding.UTF8, "application/json")
        });
        responseSequence.Enqueue(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(emptyContentResponse, Encoding.UTF8, "application/json")
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => responseSequence.Dequeue());

        // Act
        var result = await _wikipediaService.GetSectionContentAsync(topic, sectionTitle);

        // Assert
        result.Should().NotBeNull();
        result!.SectionTitle.Should().Be(sectionTitle);
        result.Content.Should().Be("No content available for this section.");
    }

    [Fact]
    public async Task GetSectionContentAsync_WithMalformedContentResponse_ShouldReturnNull()
    {
        // Arrange
        const string topic = "Python programming";
        const string sectionTitle = "History";
        
        // Mock sections response first
        const string sectionsResponse = """
            {
                "parse": {
                    "title": "Python (programming language)",
                    "sections": [
                        {
                            "toclevel": 1,
                            "level": "2",
                            "line": "History",
                            "number": "1",
                            "index": "1",
                            "anchor": "History"
                        }
                    ]
                }
            }
            """;

        // Mock malformed content response  
        const string malformedContentResponse = "{ invalid json }";

        var responseSequence = new Queue<HttpResponseMessage>();
        responseSequence.Enqueue(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(sectionsResponse, Encoding.UTF8, "application/json")
        });
        responseSequence.Enqueue(new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(malformedContentResponse, Encoding.UTF8, "application/json")
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => responseSequence.Dequeue());

        // Act
        var result = await _wikipediaService.GetSectionContentAsync(topic, sectionTitle);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task SearchAsync_WithInvalidQuery_ShouldReturnNull(string? query)
    {
        // Act
        var result = await _wikipediaService.SearchAsync(query!);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("", "History")]
    [InlineData(" ", "History")]
    [InlineData(null, "History")]
    [InlineData("Python", "")]
    [InlineData("Python", " ")]
    [InlineData("Python", null)]
    public async Task GetSectionContentAsync_WithInvalidParameters_ShouldReturnNull(string? topic, string? sectionTitle)
    {
        // Act
        var result = await _wikipediaService.GetSectionContentAsync(topic!, sectionTitle!);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task GetSectionsAsync_WithInvalidTopic_ShouldReturnNull(string? topic)
    {
        // Act
        var result = await _wikipediaService.GetSectionsAsync(topic!);

        // Assert
        result.Should().BeNull();
    }
}