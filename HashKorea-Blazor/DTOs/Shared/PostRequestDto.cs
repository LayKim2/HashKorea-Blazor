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
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    // TO DO: max size and file type
    public List<MultipartFile> ImageFiles { get; set; } = new List<MultipartFile>();
}