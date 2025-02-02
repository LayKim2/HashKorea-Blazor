using HashKorea.Extensions;
using System.ComponentModel.DataAnnotations;

namespace HashKorea.DTOs.TourMap;

public class TourMapCommentRequestDto
{
    [Required]
    public int TourMapId { get; set; }
    [Required]
    public string Comment { get; set; } = string.Empty;
}