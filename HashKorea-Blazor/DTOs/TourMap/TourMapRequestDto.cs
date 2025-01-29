using MudBlazor;
using System.ComponentModel.DataAnnotations;

namespace HashKorea.DTOs.TourMap;

public class TourMapRequestDto
{
    public int? Id { get; set; }

    //[Required]
    //public string Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string EnglishAddress { get; set; } = string.Empty;
    public string KoreanAddress { get; set; } = string.Empty;
}