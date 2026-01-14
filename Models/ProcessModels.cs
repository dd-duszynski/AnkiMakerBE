namespace AnkiMakerApp.Models;

public class ProcessUrlRequest
{
    public string Url { get; set; } = string.Empty;
}

public class ProcessUrlResponse
{
    public string Summary { get; set; } = string.Empty;
    public List<AnkiCard> AnkiCards { get; set; } = new();
}

public class AnkiCard
{
    public string Front { get; set; } = string.Empty;
    public string Back { get; set; } = string.Empty;
}
