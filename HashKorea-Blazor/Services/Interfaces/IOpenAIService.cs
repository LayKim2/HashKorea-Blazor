using HashKorea.DTOs.Auth;
using HashKorea.Responses;

namespace HashKorea.Services;

public interface IOpenAIService
{
    Task<string> GetChatGPTResponse(string prompt);
}
