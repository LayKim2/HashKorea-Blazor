﻿namespace HashKorea.DTOs.TourMap;

public class GetTourMapsResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string EnglishAddress { get; set; }
    public string KoreanAddress { get; set; }
}