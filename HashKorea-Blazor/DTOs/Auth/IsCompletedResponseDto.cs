﻿namespace HashKorea.DTOs.Auth;

public class IsCompletedResponseDto
{
    public string id { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string loginType { get; set; } = string.Empty;
    public bool isCompleted { get; set; } = false;
}