using HashKorea.DTOs.Shared;
using HashKorea.DTOs.TourMap;
using HashKorea.Responses;

namespace HashKorea.Services;

public interface ITourMapService
{
    Task<ServiceResponse<List<GetTourMapsResponseDto>>> GetTourMaps();
    Task<ServiceResponse<GetTourMapsResponseDto>> GetTourMapDetail(int Id);
    Task<ServiceResponse<int>> UpdateTourMap(TourMapRequestDto request);
}
