using HashKorea.Data;
using HashKorea.DTOs.Shared;
using HashKorea.Responses;
using Microsoft.EntityFrameworkCore;

namespace HashKorea.Services;

public class SharedService : ISharedService
{
    private readonly DataContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogService _logService;

    public SharedService(DataContext context, IHttpContextAccessor httpContextAccessor, ILogService logService)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logService = logService;
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
            var userId = 0;
            var currentUser = _httpContextAccessor.HttpContext?.User;
            if (currentUser?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = currentUser.FindFirst("Id");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int parsedUserId))
                {
                    userId = parsedUserId;
                }
            }

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

}