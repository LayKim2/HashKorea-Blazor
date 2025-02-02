using HashKorea.Common.Constants;
using HashKorea.Data;
using HashKorea.DTOs.TourMap;
using HashKorea.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HashKorea.Services;

public class TourMapService : ITourMapService
{
    private readonly DataContext _context;
    private readonly ILogService _logService;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IFileService _fileService;

    public TourMapService(DataContext context, ILogService logService, AuthenticationStateProvider authStateProvider, IFileService fileService)
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

    [AllowAnonymous]
    public async Task<ServiceResponse<List<GetTourMapsResponseDto>>> GetTourMaps()
    {
        var response = new ServiceResponse<List<GetTourMapsResponseDto>>();

        try
        {
            var tourMaps = await _context.TourMaps
                .Select(t => new GetTourMapsResponseDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Lat = t.Lat,
                    Lng = t.Lng,
                    Category = t.Category,
                    EnglishAddress = t.EnglishAddress,
                    KoreanAddress = t.KoreanAddress,
                })
                .ToListAsync();

            response.Success = true;
            response.Data = tourMaps;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Code = MessageCode.Custom.UNKNOWN_ERROR.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.UNKNOWN_ERROR];
            _logService.LogError("EXCEPTION: GetTourMaps", ex.Message, "Fetching all tour maps.");
        }

        return response;
    }

    [AllowAnonymous]
    public async Task<ServiceResponse<GetTourMapsResponseDto>> GetTourMapDetail(int Id)
    {
        var response = new ServiceResponse<GetTourMapsResponseDto>();

        try
        {
            var tourMap = await _context.TourMaps
                .Where(t => t.Id == Id)
                .Select(t => new GetTourMapsResponseDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Lat = t.Lat,
                    Lng = t.Lng,
                    Category = t.Category,
                    EnglishAddress = t.EnglishAddress,
                    KoreanAddress = t.KoreanAddress,
                })
                .FirstOrDefaultAsync();

            response.Success = true;
            response.Data = tourMap;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Code = MessageCode.Custom.UNKNOWN_ERROR.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.UNKNOWN_ERROR];
            _logService.LogError("EXCEPTION: GetTourMapDetail", ex.Message, "Fetching all tour maps.");
        }

        return response;
    }

    [Authorize]
    public async Task<ServiceResponse<int>> UpdateTourMap(TourMapRequestDto request)
    {
        var response = new ServiceResponse<int>();

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var userId = await GetUserId();
            bool isExistUser = await IsExistUser(userId);

            if (!isExistUser)
            {
                response.Success = false;
                response.Code = MessageCode.Custom.NOT_FOUND_USER.ToString();
                response.Message = MessageCode.CustomMessages[MessageCode.Custom.NOT_FOUND_USER];

                return response;
            }

            HashKorea.Models.TourMap newTourMap = new HashKorea.Models.TourMap()
            {
                UserId = userId,
                Title = request.Title,
                Lat = request.Lat,
                Lng = request.Lng,
                EnglishAddress = request.EnglishAddress,
                KoreanAddress = request.KoreanAddress,
                CategoryCD = request.CategoryCD,
                Category = request.Category,
                CreatedDate = DateTime.Now
            };

            _context.TourMaps.Add(newTourMap);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            response.Success = true;
            response.Data = newTourMap.Id;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            response.Success = false;
            response.Code = MessageCode.Custom.UNKNOWN_ERROR.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.UNKNOWN_ERROR];
            _logService.LogError("EXCEPTION: AddTourMap", ex.Message, $"user id: {await GetUserId()}");
        }

        return response;
    }

}