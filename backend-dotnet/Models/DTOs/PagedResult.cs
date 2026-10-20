using MyKopdes.Models;

namespace MyKopdes.Models.DTOs;

public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    [System.Text.Json.Serialization.JsonPropertyName("current_page")]
    public int Page { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("per_page")]
    public int PageSize { get; set; }
    public int Total { get; set; }
    [System.Text.Json.Serialization.JsonPropertyName("last_page")]
    public int TotalPages { get; set; }
}
