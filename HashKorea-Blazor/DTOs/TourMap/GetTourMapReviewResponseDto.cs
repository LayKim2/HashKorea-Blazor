using HashKorea.DTOs.Shared;
using HashKorea.Extensions;
using MudBlazor;
using System.ComponentModel.DataAnnotations;

namespace HashKorea.DTOs.TourMap;

public class GetTourMapReviewResponseDto
{
    public int Id { get; set; }
    public int TourMapId { get; set; }
    public string Initial { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public bool IsOwner { get; set; }
    public double Rating { get; set; } = 0;
    public DateTime CreatedDate { get; set; }
    public string CreatedDateRelative => CreatedDate.ToRelativeTimeString();

    public List<string> Images { get; set; } = new List<string>();
}
