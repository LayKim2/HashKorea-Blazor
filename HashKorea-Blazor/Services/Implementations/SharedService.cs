using HashKorea.Common.Constants;
using HashKorea.Data;
using HashKorea.DTOs.Shared;
using HashKorea.Models;
using HashKorea.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace HashKorea.Services;

public class SharedService : ISharedService
{
    private readonly DataContext _context;
    private readonly ILogService _logService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IFileService _fileService;

    public SharedService(DataContext context, ILogService logService, AuthenticationStateProvider authStateProvider, IFileService fileService)
    {
        _context = context;
        _logService = logService;
        _authStateProvider = authStateProvider;
        _fileService = fileService;
    }

    private async Task<bool> IsExistUser(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        return user != null;
    }

    [Authorize]
    private async Task<int> GetUserId()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var userIdClaim = authState.User.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(userIdClaim, out int userId) ? userId : 0;
    }

    private async Task<bool> CheckOwner(int userId, int postId)
    {
        var userPost = await _context.UserPosts
            .FirstOrDefaultAsync(u => u.Id == postId && u.UserId == userId);

        return userPost != null;
    }

    public async Task<ServiceResponse<List<GetPostsResponseDto>>> GetPosts(string type)
    {
        var response = new ServiceResponse<List<GetPostsResponseDto>>();

        try
        {
            var posts = await _context.UserPosts
                .Where(p => p.Type == type)
                .Select(p => new GetPostsResponseDto
                {
                    Id = p.Id,
                    //Type = p.Type,
                    Title = p.Title,
                    Category = p.Category,
                    //Content = p.Content,
                    //UserName = p.User.Name,
                    //CreatedDate = p.CreatedDate,
                })
                .ToListAsync();

            response.Data = posts;
            response.Success = true;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Failed to retrieve posts of type: {type}";
            _logService.LogError("GetPosts", ex.Message, ex.StackTrace ?? string.Empty);
        }

        return response;
    }

    public async Task<ServiceResponse<GetPostDetailResponsetDto>> GetPostDetail(int postId)
    {
        var response = new ServiceResponse<GetPostDetailResponsetDto>();

        try
        {
            var userId = await GetUserId();

            var post = await _context.UserPosts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null)
            {
                response.Success = false;
                response.Message = "Post not found";
                return response;
            }

            var postDto = new GetPostDetailResponsetDto
            {
                Id = post.Id,
                Title = post.Title,
                Content = post.Content,
                CreatedDate = post.CreatedDate,
                UserName = post.User.Name,
                UserCountry = post.User.Country,
                IsOwner = (userId != 0 && userId == post.UserId)
            };

            response.Data = postDto;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Failed to retrieve post with ID: {postId}";
            _logService.LogError("GetPostDetail", ex.Message, ex.StackTrace ?? string.Empty);
        }

        return response;
    }

    [Authorize]
    public async Task<ServiceResponse<EditPostResponseDto>> GetPostEdit(int postId)
    {
        var response = new ServiceResponse<EditPostResponseDto>();

        try
        {
            var userId = await GetUserId();

            var existingPost = await _context.UserPosts.FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId);

            if (existingPost == null)
            {
                response.Success = false;
                response.Code = MessageCode.Custom.NOT_FOUND_POST.ToString();
                response.Message = MessageCode.CustomMessages[MessageCode.Custom.NOT_FOUND_POST];
                return response;
            }

            var responseDto = new EditPostResponseDto
            {
                Id = existingPost.Id,
                Type = existingPost.Type,
                Category = existingPost.Category,
                CategoryCD = existingPost.CategoryCD,
                Title = existingPost.Title,
                Content = existingPost.Content
            };

            response.Data = responseDto;
        }
        catch (Exception ex)
        {
            response.Code = MessageCode.Custom.UNKNOWN_ERROR.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.UNKNOWN_ERROR];
            _logService.LogError("EXCEPTION: GetPostEdit", ex.Message, $"post id: {postId}");
        }

        return response;
    }


    [Authorize]
    public async Task<ServiceResponse<bool>> DeletePost(int postId)
    {
        var response = new ServiceResponse<bool>();

        var userId = await GetUserId();
        bool isExistUser = await IsExistUser(userId);

        if (isExistUser == false)
        {
            response.Success = false;
            response.Code = MessageCode.Custom.NOT_FOUND_USER.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.NOT_FOUND_USER];
            return response;
        }

        bool isOwner = await CheckOwner(userId, postId);

        if (isOwner == false)
        {
            response.Success = false;
            response.Code = MessageCode.Custom.UNAUTHORIZED.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.NOT_FOUND_USER];
            return response;
        }

        var existingPost = await _context.UserPosts
            .FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId);

        if (existingPost == null)
        {
            response.Success = false;
            response.Code = MessageCode.Custom.NOT_FOUND_POST.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.NOT_FOUND_POST];
            return response;
        }

        var existingImageUrls = await _context.UserPostImages
            .Where(i => i.PostId == postId)
            .Select(i => i.StoragePath)
            .ToListAsync();

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Remove existing images from DB
            var existingImages = await _context.UserPostImages
                .Where(i => i.PostId == postId)
                .ToListAsync();
            _context.UserPostImages.RemoveRange(existingImages);

            // Remove post
            _context.UserPosts.Remove(existingPost);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Delete S3 files after successful transaction
            foreach (var imageUrl in existingImageUrls)
            {
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    var deleteResult = await _fileService.DeleteFile(imageUrl);
                    if (!deleteResult.Success)
                    {
                        _logService.LogError("DeletePost", $"Failed to delete file from S3: {imageUrl}", "");
                    }
                }
            }

            response.Success = true;
            response.Data = true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            response.Success = false;
            response.Code = MessageCode.Custom.UNKNOWN_ERROR.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.UNKNOWN_ERROR];
            _logService.LogError("EXCEPTION: DeletePost", ex.Message, $"user id: {userId}, post id: {postId}");
        }

        return response;
    }

    [Authorize]
    public async Task<ServiceResponse<int>> AddPost(PostRequestDto model)
    {
        var response = new ServiceResponse<int>();

        var userId = 0;

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
             userId = await GetUserId();

            bool isExistUser = await IsExistUser(userId);

            if (isExistUser == false)
            {
                response.Success = false;
                response.Code = MessageCode.Custom.NOT_FOUND_USER.ToString();
                response.Message = MessageCode.CustomMessages[MessageCode.Custom.NOT_FOUND_USER];

                return response;
            }

            var convertedContentResponse = await ConvertContent(model.Content, userId);

            if (!convertedContentResponse.Success)
            {
                return new ServiceResponse<int>
                {
                    Success = false,
                    Code = convertedContentResponse.Code,
                    Message = convertedContentResponse.Message
                };
            }

            var (content, userPostImages) = convertedContentResponse.Data;

            var userPost = new UserPost
            {
                UserId = userId,
                Title = model.Title,
                Type = model.Type,
                Category = model.Category,
                CategoryCD = model.CategoryCD,
                Content = content,
                CreatedDate = DateTime.Now,
                LastUpdatedDate = DateTime.Now
            };

            _context.UserPosts.Add(userPost);
            await _context.SaveChangesAsync();

            foreach (var image in userPostImages)
            {
                image.PostId = userPost.Id;
                _context.UserPostImages.Add(image);
            }

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            response.Success = true;
            response.Data = userPost.Id;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            response.Success = false;
            response.Code = MessageCode.Custom.UNKNOWN_ERROR.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.UNKNOWN_ERROR];
            _logService.LogError("EXCEPTION: AddPost", ex.Message, $"user id: {userId}");
        }

        return response;
    }

    //public async Task<ServiceResponse<int>> UpdatePost(int userId, PostRequestDto model)
    //{
    //    var response = new ServiceResponse<int>();

    //    var postId = model.PostId ?? 0;
    //    bool isExistUser = await IsExistUser(userId);

    //    if (isExistUser == false)
    //    {
    //        response.Success = false;
    //        response.Code = MessageCode.Custom.NOT_FOUND_USER.ToString();
    //        response.Message = MessageCode.CustomMessages[MessageCode.Custom.NOT_FOUND_USER];
    //        return response;
    //    }

    //    bool isOwner = await CheckOwner(userId, postId);

    //    if (isOwner == false)
    //    {
    //        response.Success = false;
    //        response.Code = MessageCode.Custom.UNAUTHORIZED.ToString();
    //        response.Message = MessageCode.CustomMessages[MessageCode.Custom.NOT_FOUND_USER];
    //        return response;
    //    }

    //    var existingPost = await _context.UserPosts
    //        .FirstOrDefaultAsync(p => p.Id == postId && p.UserId == userId);

    //    if (existingPost == null)
    //    {
    //        response.Success = false;
    //        response.Code = MessageCode.Custom.NOT_FOUND_POST.ToString();
    //        response.Message = MessageCode.CustomMessages[MessageCode.Custom.NOT_FOUND_POST];
    //        return response;
    //    }

    //    var existingImageUrls = await _context.UserPostImages
    //                                    .Where(i => i.PostId == postId)
    //                                    .Select(i => i.StoragePath)
    //                                    .ToListAsync();

    //    using var transaction = await _context.Database.BeginTransactionAsync();

    //    try
    //    {
    //        var convertedContentResponse = await ConvertContent(model.Content, userId, model.ImageFiles);

    //        if (!convertedContentResponse.Success)
    //        {
    //            return new ServiceResponse<int>
    //            {
    //                Success = false,
    //                Code = convertedContentResponse.Code,
    //                Message = convertedContentResponse.Message
    //            };
    //        }

    //        var (content, userPostImages) = convertedContentResponse.Data;

    //        // Update existing post
    //        existingPost.Title = model.Title;
    //        existingPost.Category = model.Category;
    //        existingPost.CategoryCD = model.CategoryCD;
    //        existingPost.Content = content;
    //        existingPost.LastUpdatedDate = DateTime.Now;

    //        // Remove existing images
    //        var existingImages = await _context.UserPostImages
    //            .Where(i => i.PostId == postId)
    //            .ToListAsync();
    //        _context.UserPostImages.RemoveRange(existingImages);

    //        // Add new images
    //        foreach (var image in userPostImages)
    //        {
    //            image.PostId = postId;
    //            _context.UserPostImages.Add(image);
    //        }

    //        await _context.SaveChangesAsync();
    //        await transaction.CommitAsync();

    //        foreach (var imageUrl in existingImageUrls)
    //        {
    //            if (!string.IsNullOrEmpty(imageUrl))
    //            {
    //                var deleteResult = await _fileService.DeleteFile(imageUrl);
    //                if (!deleteResult.Success)
    //                {
    //                    _logService.LogError("UpdatePost", $"Failed to delete file from S3: {imageUrl}", "");
    //                }
    //            }
    //        }

    //        response.Success = true;
    //        response.Data = postId;
    //    }
    //    catch (Exception ex)
    //    {
    //        await transaction.RollbackAsync();
    //        response.Success = false;
    //        response.Code = MessageCode.Custom.UNKNOWN_ERROR.ToString();
    //        response.Message = MessageCode.CustomMessages[MessageCode.Custom.UNKNOWN_ERROR];
    //        _logService.LogError("EXCEPTION: UpdatePost", ex.Message, $"user id: {userId}, post id: {postId}");
    //    }

    //    return response;
    //}

    private async Task<ServiceResponse<(string Content, List<UserPostImage> Images)>> ConvertContent(string content, int userId)
    {
        var response = new ServiceResponse<(string Content, List<UserPostImage> Images)>();
        var userPostImages = new List<UserPostImage>();

        try
        {
            var regex = new Regex(@"<img[^>]*src=""data:image/(?<type>\w+);base64,(?<data>[^""]+)""[^>]*>");
            var matches = regex.Matches(content);

            var folderPath = $"content/{userId}";

            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var base64Data = match.Groups["data"].Value;
                var imageType = match.Groups["type"].Value;

                var imageBytes = Convert.FromBase64String(base64Data);
                var fileName = $"image_{Guid.NewGuid()}.{imageType}";

                using (var ms = new MemoryStream(imageBytes))
                {
                    var file = new FormFile(ms, 0, imageBytes.Length, "image", fileName)
                    {
                        Headers = new HeaderDictionary(),
                        ContentType = $"image/{imageType}"
                    };

                    var uploadResult = await _fileService.UploadFile(file, folderPath);

                    if (uploadResult.Success)
                    {
                        content = content.Replace(match.Value, $"<img src=\"{uploadResult.Data.S3Path}\" />");

                        userPostImages.Add(new UserPostImage
                        {
                            UserId = userId,
                            StoragePath = uploadResult.Data.S3Path,
                            PublicUrl = uploadResult.Data.CloudFrontUrl,
                            FileName = fileName,
                            FileType = $"image/{imageType}",
                            FileSize = imageBytes.Length
                        });
                    }
                    else
                    {
                        response.Success = false;
                        response.Code = MessageCode.Custom.UNKNOWN_ERROR.ToString();
                        response.Message = MessageCode.CustomMessages[MessageCode.Custom.UNKNOWN_ERROR];
                        _logService.LogError("ConvertContent", uploadResult.Message, $"User ID: {userId}, File Name: {fileName}");
                        return response;
                    }
                }
            }

            response.Success = true;
            response.Data = (content, userPostImages);
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Code = MessageCode.Custom.UNKNOWN_ERROR.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.UNKNOWN_ERROR];
            _logService.LogError("EXCEPTION: ConvertContent", ex.Message, $"user id: {userId}");
        }

        return response;
    }


    public async Task<ServiceResponse<List<GetCommonCodeResponseDto>>> GetCommonCodes(string type)
    {
        var response = new ServiceResponse<List<GetCommonCodeResponseDto>>();

        try
        {
            var commonCodes = await _context.CommonCodes
                .Where(cc => cc.Type == type)
                .Select(cc => new GetCommonCodeResponseDto
                {
                    Id = cc.Id,
                    Type = cc.Type,
                    Code = cc.Code,
                    Name = cc.Name,
                })
                .ToListAsync();

            if (commonCodes.Count == 0)
            {
                commonCodes.Add(new GetCommonCodeResponseDto()
                {
                    Id = 0,
                    Code = "00",
                    Name = "Default",
                    Type = type
                });
            }

            response.Data = commonCodes;
            response.Success = true;
        }
        catch (Exception ex)
        {
            response.Success = false;
            _logService.LogError("GetCommonCodes", ex.Message, ex.StackTrace ?? string.Empty);
        }

        return response;
    }
}