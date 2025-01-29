using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HashKorea.Models;

public class TourMap
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    public string Title { get; set; } = string.Empty;
    [Required]
    public double Lat { get; set; }
    [Required]
    public double Lng { get; set; }
    [Required]
    public string EnglishAddress { get; set; }
    [Required]
    public string KoreanAddress { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [ForeignKey("UserId")]
    public virtual User User { get; set; }
}
