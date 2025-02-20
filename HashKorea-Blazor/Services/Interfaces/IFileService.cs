﻿using HashKorea.DTOs.Shared;
using HashKorea.Responses;

namespace HashKorea.Services;

public interface IFileService
{
    bool IsAllowedImageFileType(string fileExtension);
    Task<ServiceResponse<(string S3Path, string CloudFrontUrl)>> UploadFile(MultipartFile file, string folderPath);
    Task<ServiceResponse<bool>> DeleteFile(string s3Path);
    Task<ServiceResponse<bool>> MoveFileToDeletedFolder(string fileName);
}