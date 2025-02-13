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


public class TourMapReviewRequestDto
{
    public int? ReviewId { get; set; }
    [Required]
    public int TourMapId { get; set; }
    [Required]
    public int UserId { get; set; }
    [Required(ErrorMessage = "Please select a rating")]
    [Range(1, 5, ErrorMessage = "Please rate your experience")]
    public int Rating { get; set; } = 0;

    [Required(ErrorMessage = "Please share your experience")]
    [MinLength(10, ErrorMessage = "Review must be at least 10 characters long")]
    public string Comment { get; set; } = string.Empty;
}