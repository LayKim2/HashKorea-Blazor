﻿using Amazon.CloudFront;
using Amazon.Runtime;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.S3;
using HashKorea.Data;
using HashKorea.Models;
using HashKorea.Responses;
using Amazon;
using HashKorea.DTOs.Shared;

namespace HashKorea.Services;

public class FileService : IFileService
{
    private readonly IAmazonS3 _s3Client;
    private readonly IAmazonCloudFront _cloudFrontClient;
    private readonly ILogService _logService;
    private readonly string _bucketName;
    private readonly string _cloudFrontDomain;
    //private readonly string _cloudFrontDistributionId; // 나중에 cache 제거를 위해 필요

    public FileService(ILogService logService)
    {
        _logService = logService;

        var credentials = FallbackCredentialsFactory.GetCredentials();
        if (credentials == null)
        {
            throw new Exception("AWS credentials not found");
        }
        else
        {
            _s3Client = new AmazonS3Client(credentials, RegionEndpoint.APNortheast2);
            _cloudFrontClient = new AmazonCloudFrontClient(credentials, RegionEndpoint.APNortheast2);
        }

        _bucketName = Environment.GetEnvironmentVariable("AWS_S3_BUCKET_NAME") ?? string.Empty;
        _cloudFrontDomain = Environment.GetEnvironmentVariable("AWS_CLOUDFRONT_DOMAIN") ?? string.Empty;

        if (string.IsNullOrEmpty(_bucketName) || string.IsNullOrEmpty(_cloudFrontDomain))
        {
            throw new Exception("AWS S3 Bucket Name or CloudFront Domain not set in environment variables");
        }
    }

    public bool IsAllowedImageFileType(string fileExtension)
    {
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        return allowedExtensions.Contains(fileExtension.ToLower());
    }

    public static IFormFile ConvertToIFormFile(MultipartFile multipartFile)
    {
        var stream = new MemoryStream(multipartFile.Content);
        return new FormFile(stream, 0, multipartFile.Content.Length, multipartFile.FileName, multipartFile.FileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = multipartFile.ContentType
        };
    }

    public async Task<ServiceResponse<(string S3Path, string CloudFrontUrl)>> UploadFile(MultipartFile file, string folderPath)
    {
        var response = new ServiceResponse<(string S3Path, string CloudFrontUrl)>();
        try
        {
            // added new one for convert image from byte[] to IFormFile
            // TO DO: Check if I need this logic
            var formFile = ConvertToIFormFile(file);

            var fileName = $"{Guid.NewGuid()}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(file.FileName)}";

            var s3Key = $"{folderPath.TrimStart('/')}/{fileName}";

            using (var fileStream = formFile.OpenReadStream())
            {
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    InputStream = fileStream,
                    Key = s3Key,
                    BucketName = _bucketName,
                    Metadata =
                {
                    ["Content-Type"] = file.ContentType
                }
                };

                var fileTransferUtility = new TransferUtility(_s3Client);
                await fileTransferUtility.UploadAsync(uploadRequest);
            }

            //var s3Path = $"s3://{_bucketName}/{s3Key}";
            var s3Path = $"https://{_bucketName}.s3.amazonaws.com/{s3Key}";
            var cloudFrontUrl = $"{_cloudFrontDomain}/{s3Key}";

            response.Data = (s3Path, cloudFrontUrl);
            response.Success = true;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = "File upload failed";
            _logService.LogError("FileUpload", ex.Message, ex.StackTrace ?? string.Empty);
        }

        return response;
    }

    public async Task<ServiceResponse<bool>> DeleteFile(string s3Path)
    {
        var response = new ServiceResponse<bool>();
        try
        {
            var uri = new Uri(s3Path);
            string bucketName = uri.Host;
            string s3Key = uri.AbsolutePath.TrimStart('/');

            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = s3Key
            };

            await _s3Client.DeleteObjectAsync(deleteObjectRequest);

            response.Data = true;
            response.Success = true;
        }
        catch (AmazonS3Exception ex)
        {
            response.Success = false;
            response.Message = "Failed to delete file.";
            _logService.LogError("DeleteFile", ex.Message, ex.StackTrace ?? string.Empty);
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = "Failed to delete file.";
            _logService.LogError("DeleteFile", ex.Message, ex.StackTrace ?? string.Empty);
        }

        return response;
    }

    public async Task<ServiceResponse<bool>> MoveFileToDeletedFolder(string fileName)
    {
        var response = new ServiceResponse<bool>();
        try
        {
            var sourceKey = fileName;
            var destinationKey = $"deleted/{fileName}";

            var copyRequest = new CopyObjectRequest
            {
                SourceBucket = _bucketName,
                SourceKey = sourceKey,
                DestinationBucket = _bucketName,
                DestinationKey = destinationKey
            };

            await _s3Client.CopyObjectAsync(copyRequest);

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = sourceKey
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);

            response.Data = true;
            response.Success = true;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = "Failed to move file to deleted folder";
            _logService.LogError("MoveFileToDeletedFolder", ex.Message, ex.StackTrace ?? string.Empty);
        }
        return response;
    }
}