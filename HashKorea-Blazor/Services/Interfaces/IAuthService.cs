using HashKorea.DTOs.Auth;
using HashKorea.Responses;

namespace HashKorea.Services;

public interface IAuthService
{
    Task<ServiceResponse<IsCompletedResponseDto>> IsCompleted(IsCompletedRequestDto model);
}
