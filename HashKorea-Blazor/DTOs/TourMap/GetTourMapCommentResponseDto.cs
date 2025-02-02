using HashKorea.Extensions;

namespace HashKorea.DTOs.TourMap;

public class GetTourMapCommentResponseDto
{
    public int Id { get; set; }
    public string Initial { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }

    public string CreatedDateRelative => CreatedDate.ToRelativeTimeString();
}