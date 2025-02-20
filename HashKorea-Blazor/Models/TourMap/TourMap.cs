﻿using System.ComponentModel.DataAnnotations.Schema;
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
    public string KoreanAddress { get; set; } = string.Empty;
    [Required]
    public string Category { get; set; }
    [Required]
    public string CategoryCD { get; set; }
    public string MainImagePublicUrl { get; set; } = string.Empty;
    public string MainImageStoragePath { get; set; } = string.Empty;
    public double AverageRating { get; set; } = 0;

    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public virtual ICollection<TourMapComment> TourMapComments { get; set; } = new HashSet<TourMapComment>();
    public virtual ICollection<TourMapReview> TourMapReviews { get; set; } = new HashSet<TourMapReview>();

    [ForeignKey("UserId")]
    public virtual User User { get; set; }
}
