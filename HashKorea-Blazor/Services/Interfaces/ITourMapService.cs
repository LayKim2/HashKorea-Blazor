using HashKorea.DTOs.Shared;
using HashKorea.DTOs.TourMap;
using HashKorea.Responses;

namespace HashKorea.Services;

public interface ITourMapService
{
    Task<ServiceResponse<List<GetTourMapsResponseDto>>> GetTourMaps();
    Task<ServiceResponse<GetTourMapsResponseDto>> GetTourMapDetail(int Id);
    Task<ServiceResponse<int>> UpdateTourMap(TourMapRequestDto request);

    #region comment
    Task<ServiceResponse<int>> AddTourMapComment(TourMapCommentRequestDto request);
    Task<ServiceResponse<List<GetTourMapCommentResponseDto>>> GetTourMapComments(int tourMapId);
    #endregion


    #region review
    Task<ServiceResponse<List<GetTourMapReviewResponseDto>>> GetTourMapReviews(int tourMapId);
    Task<ServiceResponse<int>> AddOrUpdateTourMapReview(TourMapReviewRequestDto request);
    Task<ServiceResponse<bool>> DeleteTourMapReview(int reviewId);
    #endregion

}
