using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using WikipediaMcpServer.Models;

namespace WikipediaMcpServer.Services;

public interface IWikipediaService
{
    Task<WikipediaSearchResult?> SearchAsync(string query);
    Task<WikipediaSectionsResult?> GetSectionsAsync(string topic);
    Task<WikipediaSectionContentResult?> GetSectionContentAsync(string topic, string sectionTitle);
}

public class WikipediaService : IWikipediaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WikipediaService> _logger;
    private const string WikipediaApiUrl = "https://en.wikipedia.org/api/rest_v1";

    public WikipediaService(HttpClient httpClient, ILogger<WikipediaService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", 
            "WikipediaMcpServer/1.0 (https://github.com/wikipedia-mcp-dotnet-server)");
    }

    public async Task<WikipediaSearchResult?> SearchAsync(string query)
    {
        try
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var searchUrl = $"{WikipediaApiUrl}/page/summary/{encodedQuery}";

            _logger.LogInformation("🔍 Wikipedia Search Request: {Url}", searchUrl);
            
            var response = await _httpClient.GetAsync(searchUrl);

            _logger.LogInformation("📡 Wikipedia Search Response: Status={Status}, Query='{Query}'", 
                response.StatusCode, query);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Wikipedia search failed for query: {Query}. Status: {Status}", 
                    query, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("📥 Wikipedia Search Content Length: {Length} characters", content.Length);
            
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var summary = JsonSerializer.Deserialize<WikipediaSummaryResponse>(content, options);

            if (summary == null)
            {
                return null;
            }

            return new WikipediaSearchResult
            {
                Title = summary.Title,
                Summary = summary.Extract ?? "No summary available.",
                Url = summary.ContentUrls?.Desktop?.Page ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching Wikipedia for query: {Query}", query);
            return null;
        }
    }

    public async Task<WikipediaSectionsResult?> GetSectionsAsync(string topic)
    {
        try
        {
            var encodedTopic = Uri.EscapeDataString(topic);
            var sectionsUrl = $"https://en.wikipedia.org/w/api.php?action=parse&page={encodedTopic}&prop=sections&format=json";

            _logger.LogInformation("📑 Wikipedia Sections Request: {Url}", sectionsUrl);
            
            var response = await _httpClient.GetAsync(sectionsUrl);

            _logger.LogInformation("📡 Wikipedia Sections Response: Status={Status}, Topic='{Topic}'", 
                response.StatusCode, topic);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Wikipedia sections request failed for topic: {Topic}. Status: {Status}", 
                    topic, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("📥 Wikipedia Sections Content Length: {Length} characters", content.Length);
            
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var apiResponse = JsonSerializer.Deserialize<WikipediaApiSectionResponse>(content, options);

            if (apiResponse?.Parse == null)
            {
                return null;
            }

            // Parse the actual sections from the API response
            var sections = new List<string>();
            
            if (apiResponse.Parse.Sections != null)
            {
                foreach (var section in apiResponse.Parse.Sections)
                {
                    if (!string.IsNullOrEmpty(section.Line))
                    {
                        // Format section with indentation based on level
                        var indent = new string(' ', (section.TocLevel - 1) * 2);
                        sections.Add($"{indent}{section.Line}");
                    }
                }
            }

            // If no sections found, add a default message
            if (sections.Count == 0)
            {
                sections.Add("No sections available");
            }

            return new WikipediaSectionsResult
            {
                Title = apiResponse.Parse.Title,
                Sections = sections,
                Url = $"https://en.wikipedia.org/wiki/{encodedTopic}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sections for topic: {Topic}", topic);
            return null;
        }
    }

    public async Task<WikipediaSectionContentResult?> GetSectionContentAsync(string topic, string sectionTitle)
    {
        try
        {
            var encodedTopic = Uri.EscapeDataString(topic);
            
            // First, get all sections to find the section index
            var sectionsUrl = $"https://en.wikipedia.org/w/api.php?action=parse&page={encodedTopic}&prop=sections&format=json";
            
            _logger.LogInformation("📖 Wikipedia Section Content Request (Step 1): {Url}", sectionsUrl);
            
            var sectionsResponse = await _httpClient.GetAsync(sectionsUrl);
            
            if (!sectionsResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Wikipedia sections request failed for topic: {Topic}. Status: {Status}", 
                    topic, sectionsResponse.StatusCode);
                return null;
            }

            var sectionsContent = await sectionsResponse.Content.ReadAsStringAsync();
            var sectionsOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var sectionsApiResponse = JsonSerializer.Deserialize<WikipediaApiSectionResponse>(sectionsContent, sectionsOptions);
            
            if (sectionsApiResponse?.Parse?.Sections == null)
            {
                _logger.LogWarning("No sections found for topic: {Topic}", topic);
                return null;
            }

            // Find the section by title (case-insensitive, handle indentation)
            var targetSection = sectionsApiResponse.Parse.Sections.FirstOrDefault(s => 
                string.Equals(s.Line.Trim(), sectionTitle.Trim(), StringComparison.OrdinalIgnoreCase));

            if (targetSection == null)
            {
                _logger.LogWarning("Section '{Section}' not found in topic: {Topic}", sectionTitle, topic);
                return new WikipediaSectionContentResult
                {
                    SectionTitle = sectionTitle,
                    Content = $"Section '{sectionTitle}' not found. Available sections: {string.Join(", ", sectionsApiResponse.Parse.Sections.Take(5).Select(s => s.Line))}"
                };
            }

            // Get the section content using the section index
            var contentUrl = $"https://en.wikipedia.org/w/api.php?action=parse&page={encodedTopic}&section={targetSection.Index}&prop=text&format=json";
            
            _logger.LogInformation("📖 Wikipedia Section Content Request (Step 2): {Url}", contentUrl);
            
            var contentResponse = await _httpClient.GetAsync(contentUrl);

            _logger.LogInformation("📡 Wikipedia Section Content Response: Status={Status}, Topic='{Topic}', Section='{Section}'", 
                contentResponse.StatusCode, topic, sectionTitle);

            if (!contentResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Wikipedia section content request failed for topic: {Topic}, section: {Section}. Status: {Status}", 
                    topic, sectionTitle, contentResponse.StatusCode);
                return null;
            }

            var contentText = await contentResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("📥 Wikipedia Section Content Length: {Length} characters", contentText.Length);
            var parseResponse = JsonSerializer.Deserialize<WikipediaApiParseTextResponse>(contentText, sectionsOptions);

            if (parseResponse?.Parse?.Text == null)
            {
                return new WikipediaSectionContentResult
                {
                    SectionTitle = sectionTitle,
                    Content = "No content available for this section."
                };
            }

            // Extract the HTML content and convert to plain text
            var htmlContent = parseResponse.Parse.Text.ContainsKey("*") ? parseResponse.Parse.Text["*"] : "";
            var plainTextContent = ConvertHtmlToPlainText(htmlContent);

            return new WikipediaSectionContentResult
            {
                SectionTitle = sectionTitle,
                Content = plainTextContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting section content for topic: {Topic}, section: {Section}", 
                topic, sectionTitle);
            return null;
        }
    }

    private static string ConvertHtmlToPlainText(string html)
    {
        if (string.IsNullOrEmpty(html))
            return "";

        // Simple HTML to text conversion - removes HTML tags and decodes entities
        var plainText = html;
        
        // Remove HTML tags
        plainText = System.Text.RegularExpressions.Regex.Replace(plainText, "<[^>]*>", "");
        
        // Decode common HTML entities
        plainText = plainText.Replace("&amp;", "&")
                            .Replace("&lt;", "<")
                            .Replace("&gt;", ">")
                            .Replace("&quot;", "\"")
                            .Replace("&#39;", "'")
                            .Replace("&nbsp;", " ");
        
        // Clean up extra whitespace
        plainText = System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+", " ");
        plainText = plainText.Trim();
        
        // Limit length to avoid extremely long responses
        if (plainText.Length > 2000)
        {
            plainText = plainText.Substring(0, 2000) + "... [Content truncated]";
        }
        
        return plainText;
    }
}

// Additional DTO for Wikipedia summary response
public class WikipediaSummaryResponse
{
    public string Title { get; set; } = string.Empty;
    public string? Extract { get; set; }
    
    [JsonPropertyName("content_urls")]
    public WikipediaContentUrls? ContentUrls { get; set; }
}

public class WikipediaContentUrls
{
    public WikipediaDesktop? Desktop { get; set; }
}

public class WikipediaDesktop
{
    public string Page { get; set; } = string.Empty;
}