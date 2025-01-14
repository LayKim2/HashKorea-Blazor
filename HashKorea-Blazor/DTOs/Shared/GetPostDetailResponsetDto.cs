using HashKorea.Extensions;

namespace HashKorea.DTOs.Shared;

public class GetPostDetailResponsetDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserCountry { get; set; } = string.Empty;
    public bool IsOwner { get; set; } = false;
    public string RelativeTime => CreatedDate.ToRelativeTimeString();
}