using HashKorea.Common.Constants;
using HashKorea.Data;
using HashKorea.DTOs.Shared;
using HashKorea.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
}