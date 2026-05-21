using System.Text.Json.Serialization;

namespace TreeCoursework;

public class GenerateTreeRequest
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("orientation")]
    public string Orientation { get; set; } = "vertical";

    [JsonPropertyName("max_depth")]
    public int MaxDepth { get; set; }

    [JsonPropertyName("child_probability")]
    public double ChildProbability { get; set; }

    [JsonPropertyName("seed")]
    public int Seed { get; set; }
}

public class APIEnvelope<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; } = default!;
}

public class TreeSummary
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("orientation")]
    public string Orientation { get; set; } = "vertical";

    [JsonPropertyName("max_depth")]
    public int MaxDepth { get; set; }

    [JsonPropertyName("child_probability")]
    public double ChildProbability { get; set; }

    [JsonPropertyName("seed")]
    public int Seed { get; set; }

    [JsonPropertyName("node_count")]
    public int NodeCount { get; set; }

    [JsonPropertyName("generated_at")]
    public DateTime? GeneratedAt { get; set; }

    public string DisplayTitle => !string.IsNullOrEmpty(Title) ? Title : $"Дерево #{Id}";

    public string Subtitle =>
        $"{(Orientation == "vertical" ? "Вертикально" : "Горизонтально")} • " +
        $"глубина {MaxDepth} • {NodeCount} узл. • " +
        $"{GeneratedAt?.ToString("dd.MM.yyyy HH:mm") ?? "без времени"}";
}

public class TreeRecord
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("orientation")]
    public string Orientation { get; set; } = "vertical";

    [JsonPropertyName("max_depth")]
    public int MaxDepth { get; set; }

    [JsonPropertyName("child_probability")]
    public double ChildProbability { get; set; }

    [JsonPropertyName("seed")]
    public int Seed { get; set; }

    [JsonPropertyName("node_count")]
    public int NodeCount { get; set; }

    [JsonPropertyName("generated_at")]
    public DateTime? GeneratedAt { get; set; }

    [JsonPropertyName("root")]
    public TreeNode? Root { get; set; }

    public string DisplayTitle => !string.IsNullOrEmpty(Title) ? Title : $"Дерево #{Id}";
}

public class TreeNode
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("number")]
    public int Number { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("depth")]
    public int Depth { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("child_slot")]
    public int? ChildSlot { get; set; }

    [JsonPropertyName("child_numbers")]
    public List<int> ChildNumbers { get; set; } = new();

    [JsonPropertyName("children")]
    public List<TreeNode> Children { get; set; } = new();

    public string ChildNumbersText => ChildNumbers.Count == 0
        ? "Дочерние: нет"
        : $"Дочерние: {string.Join(", ", ChildNumbers)}";
}

public class LogTreeReference
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    public string DisplayTitle => !string.IsNullOrEmpty(Title) ? Title : $"Дерево #{Id}";
}

public class RequestLogEntry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [JsonPropertyName("method")]
    public string Method { get; set; } = "";

    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = "";

    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("status_code")]
    public int StatusCode { get; set; }

    [JsonPropertyName("tree")]
    public LogTreeReference? Tree { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }
}
