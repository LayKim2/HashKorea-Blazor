﻿namespace HashKorea.DTOs.TourMap;

public class GetTourMapsResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Category { get; set; } = string.Empty;
    public string EnglishAddress { get; set; }
    public string KoreanAddress { get; set; }
    public double AverageRating { get; set; } = 0;
    public int NumberOfReviews { get; set; } = 0;

    public List<GetTourMapCommentResponseDto> Comments { get; set; } = new List<GetTourMapCommentResponseDto>();
    public List<GetTourMapReviewResponseDto> Reviews { get; set; } = new List<GetTourMapReviewResponseDto>();
}