using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HashKorea.Models;

public class TourMapReviewImage
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [Required]
    public int TourMapId { get; set; }
    [Required]
    public int UserId { get; set; }
    [Required]
    public int ReviewId { get; set; }
    public string MainImagePublicUrl { get; set; } = string.Empty;
    public string MainImageStoragePath { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime UpdatedDate { get; set; } = DateTime.Now;

    [ForeignKey("UserId")]
    public virtual User User { get; set; }
    [ForeignKey("ReviewId")]
    public virtual TourMapReview TourMapReview { get; set; }

}