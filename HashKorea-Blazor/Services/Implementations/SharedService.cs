using HashKorea.Data;
using HashKorea.DTOs.Shared;
using HashKorea.Responses;
using Microsoft.EntityFrameworkCore;

namespace HashKorea.Services;

public class SharedService : ISharedService
{
    private readonly DataContext _context; 
    private readonly ILogService _logService;

    public SharedService(DataContext context, ILogService logService)
    {
        _context = context;
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

}