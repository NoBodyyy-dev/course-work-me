using System.Text;
using System.Text.Json;

namespace TreeCoursework;

public class ApiException : Exception
{
    public int StatusCode { get; }
    public ApiException(int statusCode, string message) : base(message) => StatusCode = statusCode;
}

public class ApiClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<TreeRecord> FetchLatestTreeAsync(string baseUrl)
        => GetAsync<TreeRecord>(baseUrl, "trees/latest");

    public Task<List<TreeSummary>> FetchTreesAsync(string baseUrl, int limit = 15)
        => GetAsync<List<TreeSummary>>(baseUrl, $"trees?limit={limit}");

    public Task<TreeRecord> FetchTreeAsync(string baseUrl, int id)
        => GetAsync<TreeRecord>(baseUrl, $"trees/{id}");

    public Task<List<RequestLogEntry>> FetchLogsAsync(string baseUrl, int limit = 15)
        => GetAsync<List<RequestLogEntry>>(baseUrl, $"request-logs?limit={limit}");

    public async Task<TreeRecord> GenerateTreeAsync(string baseUrl, GenerateTreeRequest req)
    {
        var url = BuildUrl(baseUrl, "trees/generate");
        var body = new StringContent(JsonSerializer.Serialize(req, Opts), Encoding.UTF8, "application/json");
        var resp = await Http.PostAsync(url, body);
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new ApiException((int)resp.StatusCode, ExtractError(json));
        return JsonSerializer.Deserialize<APIEnvelope<TreeRecord>>(json, Opts)!.Data;
    }

    private async Task<T> GetAsync<T>(string baseUrl, string path)
    {
        var url = BuildUrl(baseUrl, path);
        var resp = await Http.GetAsync(url);
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new ApiException((int)resp.StatusCode, ExtractError(json));
        return JsonSerializer.Deserialize<APIEnvelope<T>>(json, Opts)!.Data;
    }

    private static string BuildUrl(string baseUrl, string path)
        => $"{baseUrl.TrimEnd('/')}/{path}";

    private static string ExtractError(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("message", out var m) && m.GetString() is { Length: > 0 } s)
                return s;
        }
        catch { }
        return "Не удалось выполнить запрос.";
    }
}
