﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HashKorea.Models;

public class TourMapReview
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    [Required]
    public int TourMapId { get; set; }
    [Required]
    public int UserId { get; set; }
    [Required]
    public string Comment { get; set; } = string.Empty;
    [Required]
    public double Rating { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime UpdatedDate { get; set; } = DateTime.Now;

    [ForeignKey("UserId")]
    public virtual User User { get; set; }

    public virtual ICollection<TourMapReviewImage> TourMapReviewImages { get; set; } = new HashSet<TourMapReviewImage>();
}
