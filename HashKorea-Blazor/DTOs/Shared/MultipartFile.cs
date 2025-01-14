﻿using System.ComponentModel.DataAnnotations;

namespace HashKorea.DTOs.Shared;

public class MultipartFile
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}