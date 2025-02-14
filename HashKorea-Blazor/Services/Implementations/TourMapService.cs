using HashKorea.Common.Constants;
using HashKorea.Data;
using HashKorea.DTOs.TourMap;
using HashKorea.Models;
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
                    AverageRating = t.AverageRating,
                    NumberOfReviews = t.TourMapReviews.Count()
                })
                .FirstOrDefaultAsync();

            if (tourMap != null)
            {
                var getComments = await GetTourMapComments(Id);
                var getReviews = await GetTourMapReviews(Id);

                if (getComments.Success && getComments.Data != null)
                {
                    tourMap.Comments = getComments.Data;
                }

                if (getReviews.Success && getReviews.Data != null)
                {
                    tourMap.Reviews = getReviews.Data;
                }
            }

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

    [AllowAnonymous]
    public async Task<ServiceResponse<List<GetTourMapCommentResponseDto>>> GetTourMapComments(int tourMapId)
    {
        var response = new ServiceResponse<List<GetTourMapCommentResponseDto>>();

        try
        {
            var comments = await _context.TourMapComments   
                .Where(c => c.TourMapId == tourMapId)
                .OrderByDescending(c => c.CreatedDate)
                .Select(c => new GetTourMapCommentResponseDto
                {
                    Id = c.Id,
                    UserName = c.User.Name,
                    Initial = c.User.Name.Substring(0, 1),
                    Comment = c.Comment,
                    CreatedDate = c.CreatedDate
                })
                .ToListAsync();

            response.Success = true;
            response.Data = comments;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Code = MessageCode.Custom.UNKNOWN_ERROR.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.UNKNOWN_ERROR];
            _logService.LogError("EXCEPTION: GetTourMapComments", ex.Message, "Fetching comments for tour map.");
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

    public async Task<ServiceResponse<int>> AddTourMapComment(TourMapCommentRequestDto request)
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

            bool isExistTourMap = await _context.TourMaps.AnyAsync(t => t.Id == request.TourMapId);
            if (!isExistTourMap)
            {
                response.Success = false;
                response.Code = MessageCode.Custom.NOT_FOUND_DATA.ToString();
                response.Message = MessageCode.CustomMessages[MessageCode.Custom.NOT_FOUND_DATA];

                return response;
            }

            TourMapComment newComment = new TourMapComment()
            {
                UserId = userId,
                TourMapId = request.TourMapId,
                Comment = request.Comment,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now
            };

            _context.TourMapComments.Add(newComment);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            response.Success = true;
            response.Data = newComment.Id;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            response.Success = false;
            response.Code = MessageCode.Custom.UNKNOWN_ERROR.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.UNKNOWN_ERROR];
            _logService.LogError("EXCEPTION: AddTourMapComment", ex.Message, $"user id: {await GetUserId()}");
        }

        return response;
    }


    #region review
    public async Task<ServiceResponse<GetTourMapReviewResponseDto>> GetTourMapReview(int reviewId)
    {
        var response = new ServiceResponse<GetTourMapReviewResponseDto>();

        try
        {
            var currentUserId = await GetUserId();
            var reviews = await _context.TourMapReviews
                .Where(r => r.Id == reviewId)
                .Select(r => new GetTourMapReviewResponseDto
                {
                    Id = r.Id,
                    UserName = r.User.Name,
                    Initial = r.User.Name.Substring(0, 1),
                    TourMapId = r.TourMapId,
                    Comment = r.Comment,
                    Rating = r.Rating,
                    IsOwner = r.UserId == currentUserId,
                    CreatedDate = r.CreatedDate
                })
                .FirstOrDefaultAsync();

            response.Success = true;
            response.Data = reviews;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Code = MessageCode.Custom.UNKNOWN_ERROR.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.UNKNOWN_ERROR];
            _logService.LogError("EXCEPTION: GetTourMapReview", ex.Message, "Fetching reviews");
        }

        return response;
    }

    public async Task<ServiceResponse<List<GetTourMapReviewResponseDto>>> GetTourMapReviews(int tourMapId)
    {
        var response = new ServiceResponse<List<GetTourMapReviewResponseDto>>();

        try
        {
            var currentUserId = await GetUserId();

            var reviews = await _context.TourMapReviews
                .Where(r => r.TourMapId == tourMapId)
                .OrderByDescending(c => c.CreatedDate)
                .Select(r => new GetTourMapReviewResponseDto
                {
                    Id = r.Id,
                    UserName = r.User.Name,
                    Initial = r.User.Name.Substring(0, 1),
                    TourMapId = r.TourMapId,
                    Comment = r.Comment,
                    Rating = r.Rating,
                    IsOwner = r.UserId == currentUserId,
                    CreatedDate = r.CreatedDate
                })
                .ToListAsync();

            response.Success = true;
            response.Data = reviews;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Code = MessageCode.Custom.UNKNOWN_ERROR.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.UNKNOWN_ERROR];
            _logService.LogError("EXCEPTION: GetTourMapReviews", ex.Message, "Fetching reviews");
        }

        return response;
    }

    [Authorize]
    public async Task<ServiceResponse<int>> AddOrUpdateTourMapReview(TourMapReviewRequestDto request)
    {
        var response = new ServiceResponse<int>();

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var userId = await GetUserId();

            if (userId == 0)
            {
                response.Success = false;
                response.Code = MessageCode.Custom.NOT_FOUND_USER.ToString();
                response.Message = MessageCode.CustomMessages[MessageCode.Custom.NOT_FOUND_USER];
                return response;
            }

            TourMapReview review;

            if (request.ReviewId.HasValue)
            {
                review = await _context.TourMapReviews
                    .FirstOrDefaultAsync(r => r.Id == request.ReviewId.Value && r.UserId == userId);

                if (review == null)
                {
                    response.Success = false;
                    response.Code = MessageCode.Custom.NOT_FOUND_DATA.ToString();
                    response.Message = MessageCode.CustomMessages[MessageCode.Custom.NOT_FOUND_DATA];
                    return response;
                }

                review.TourMapId = request.TourMapId;
                review.Comment = request.Comment;
                review.Rating = request.Rating;
                review.UpdatedDate = DateTime.UtcNow;
            }
            else
            {
                review = new TourMapReview
                {
                    UserId = userId,
                    TourMapId = request.TourMapId,
                    Comment = request.Comment,
                    Rating = request.Rating,
                    CreatedDate = DateTime.UtcNow
                };

                _context.TourMapReviews.Add(review);
            }

            await _context.SaveChangesAsync();

            var tourMap = await _context.TourMaps
                .FirstOrDefaultAsync(t => t.Id == request.TourMapId);

            if (tourMap != null)
            {
                var totalReviews = await _context.TourMapReviews
                    .Where(r => r.TourMapId == request.TourMapId)
                    .CountAsync();

                if (totalReviews > 0)
                {
                    var totalRating = await _context.TourMapReviews
                        .Where(r => r.TourMapId == request.TourMapId)
                        .SumAsync(r => r.Rating);

                    tourMap.AverageRating = totalRating / totalReviews;
                    await _context.SaveChangesAsync();
                }
                else
                {
                    tourMap.AverageRating = 0;
                    await _context.SaveChangesAsync();
                }
            }

            await transaction.CommitAsync();

            response.Success = true;
            response.Data = review.Id;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            response.Success = false;
            response.Code = MessageCode.Custom.UNKNOWN_ERROR.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.UNKNOWN_ERROR];
            _logService.LogError("EXCEPTION: AddOrUpdateTourMapReview", ex.Message, $"user id: {await GetUserId()}");
        }

        return response;
    }


    [Authorize]
    public async Task<ServiceResponse<bool>> DeleteTourMapReview(int reviewId)
    {
        var response = new ServiceResponse<bool>();
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var userId = await GetUserId();
            var review = await _context.TourMapReviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

            if (review == null)
            {
                response.Success = false;
                response.Code = MessageCode.Custom.NOT_FOUND_DATA.ToString();
                response.Message = MessageCode.CustomMessages[MessageCode.Custom.NOT_FOUND_DATA];
                return response;
            }

            _context.TourMapReviews.Remove(review);

            var tourMap = await _context.TourMaps
                .Include(t => t.TourMapReviews)
                .FirstOrDefaultAsync(t => t.Id == review.TourMapId);

            if (tourMap == null)
            {
                response.Success = false;
                response.Code = MessageCode.Custom.NOT_FOUND_DATA.ToString();
                response.Message = MessageCode.CustomMessages[MessageCode.Custom.NOT_FOUND_DATA];
                return response;
            }

            var remainingReviews = tourMap.TourMapReviews.ToList();
            if (remainingReviews.Any())
            {
                tourMap.AverageRating = remainingReviews.Average(r => r.Rating);
            }
            else
            {
                tourMap.AverageRating = 0; 
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            response.Success = true;
            response.Data = true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            response.Success = false;
            response.Code = MessageCode.Custom.UNKNOWN_ERROR.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.UNKNOWN_ERROR];
            _logService.LogError("EXCEPTION: DeleteTourMapReview", ex.Message, $"user id: {await GetUserId()}");
        }

        return response;
    }

    #endregion

}