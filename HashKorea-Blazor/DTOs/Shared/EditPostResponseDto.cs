using System.ComponentModel.DataAnnotations;

namespace HashKorea.DTOs.Shared;

public class EditPostResponseDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CategoryCD { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string LocationCD { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}