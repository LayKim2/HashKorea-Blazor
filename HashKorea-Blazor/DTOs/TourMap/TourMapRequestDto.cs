using HashKorea.DTOs.Shared;
using MudBlazor;
using System.ComponentModel.DataAnnotations;

namespace HashKorea.DTOs.TourMap;

public class TourMapRequestDto
{
    //public int? Id { get; set; }

    public string Title { get; set; } = string.Empty;
    [Required]
    public double Lat { get; set; }
    [Required]
    public double Lng { get; set; }
    [Required]
    public string EnglishAddress { get; set; } = string.Empty;
    public string KoreanAddress { get; set; } = string.Empty;
    [Required]
    public string Category { get; set; }
    [Required]
    public string CategoryCD { get; set; }

    public void SetCategory(string code, List<GetCommonCodeResponseDto> categories)
    {
        var selectedCategory = categories.FirstOrDefault(c => c.Code == code);
        Category = selectedCategory?.Name ?? string.Empty;
        CategoryCD = code;
    }
}