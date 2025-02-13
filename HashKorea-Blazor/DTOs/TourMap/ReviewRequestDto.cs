namespace HashKorea.DTOs.TourMap;

public class ReviewRequestDto
{
    public int? ReviewId { get; set; }
    public int TypeId { get; set; }
    public string Comment { get; set; } = string.Empty;
}