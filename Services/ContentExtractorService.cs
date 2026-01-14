using HtmlAgilityPack;
using System.Text;

namespace AnkiMakerApp.Services;

public interface IContentExtractorService
{
    Task<string> ExtractContentAsync(string url);
}

public class ContentExtractorService : IContentExtractorService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ContentExtractorService> _logger;

    public ContentExtractorService(HttpClient httpClient, ILogger<ContentExtractorService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> ExtractContentAsync(string url)
    {
        try
        {
            var html = await _httpClient.GetStringAsync(url);
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            // Usuń skrypty, style i inne niepotrzebne elementy
            var nodesToRemove = htmlDoc.DocumentNode.SelectNodes("//script|//style|//nav|//footer|//header|//aside");
            if (nodesToRemove != null)
            {
                foreach (var node in nodesToRemove)
                {
                    node.Remove();
                }
            }

            // Spróbuj znaleźć główną treść artykułu
            var articleNodes = htmlDoc.DocumentNode.SelectNodes("//article|//main|//*[@class='content']|//*[@class='post']");

            string content;
            if (articleNodes != null && articleNodes.Any())
            {
                content = string.Join("\n", articleNodes.Select(n => n.InnerText));
            }
            else
            {
                // Jeśli nie znaleziono typowych znaczników artykułu, weź całą treść body
                var bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
                content = bodyNode?.InnerText ?? htmlDoc.DocumentNode.InnerText;
            }

            // Oczyszczanie tekstu
            content = System.Net.WebUtility.HtmlDecode(content);
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ");
            content = content.Trim();

            _logger.LogInformation("Extracted {Length} characters from {Url}", content.Length, url);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting content from {Url}", url);
            throw;
        }
    }
}
