using MudBlazor;
using System.ComponentModel.DataAnnotations;

namespace HashKorea.DTOs.Shared;

public class PostRequestDto
{
    public int? PostId { get; set; }

    [Required]
    public string Type { get; set; }
    [Required]
    public string Category { get; set; }
    [Required]
    public string CategoryCD { get; set; }

    [Required]
    public string Location { get; set; }
    [Required]
    public string LocationCD { get; set; }

    [Required]
    public DateRange DateRange { get; set; }

    [Required]
    public DateOnly StartDate => DateOnly.FromDateTime(DateRange.Start.GetValueOrDefault());

    [Required]
    public DateOnly EndDate => DateOnly.FromDateTime(DateRange.End.GetValueOrDefault());

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; }

    // TO DO: add required
    //[Required]
    public string Content { get; set; } = string.Empty;

    // TO DO: max size and file type
    public List<MultipartFile> ImageFiles { get; set; } = new List<MultipartFile>();

    public void SetCategory(string code, List<GetCommonCodeResponseDto> categories)
    {
        var selectedCategory = categories.FirstOrDefault(c => c.Code == code);
        Category = selectedCategory?.Name ?? string.Empty;
        CategoryCD = code;
    }

    public void SetLocation(string code, List<GetCommonCodeResponseDto> locations)
    {
        var selectedLocation = locations.FirstOrDefault(l => l.Code == code);
        Location = selectedLocation?.Name ?? string.Empty;
        LocationCD = code;
    }

    public void SetDateRange(DateRange dateRange)
    {
        DateRange = dateRange;
    }
}