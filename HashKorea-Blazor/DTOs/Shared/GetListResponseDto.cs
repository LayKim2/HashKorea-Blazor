using HashKorea.Extensions;

namespace HashKorea.DTOs.Shared;

public class GetPostsResponseDto
{
    public int Id { get; set; }
    public string MainImageUrl { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string RelativeTime => CreatedDate.ToRelativeTimeString();

}