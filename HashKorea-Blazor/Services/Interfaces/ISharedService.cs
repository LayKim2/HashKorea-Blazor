using HashKorea.DTOs.Shared;
using HashKorea.Responses;

namespace HashKorea.Services;

public interface ISharedService
{
    Task<ServiceResponse<List<GetPostsResponseDto>>> GetPosts(string type);
    Task<ServiceResponse<GetPostDetailResponsetDto>> GetPostDetail(int postId);
    Task<ServiceResponse<bool>> DeletePost(int postId);
    Task<ServiceResponse<int>> AddPost(PostRequestDto model);
    Task<ServiceResponse<List<GetCommonCodeResponseDto>>> GetCommonCodes(string type);
}
