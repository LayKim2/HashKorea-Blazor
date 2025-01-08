using HashKorea.Common.Constants;
using HashKorea.Data;
using HashKorea.DTOs.Auth;
using HashKorea.Models;
using HashKorea.Responses;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HashKorea.Services;

public class AuthService : IAuthService
{
    private readonly DataContext _context;
    private readonly ILogService _logService;

    public AuthService(DataContext context, ILogService logService, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _logService = logService;
    }

    public async Task<ServiceResponse<IsCompletedResponseDto>> IsCompleted(IsCompletedRequestDto model)
    {
        var response = new ServiceResponse<IsCompletedResponseDto>();

        try
        {
            var userAuth = await _context.UserAuth.FirstOrDefaultAsync(ua => ua.AuthKey == model.SignInType && ua.AuthValue == model.Id);

            if (userAuth == null)
            {
                var newUser = new User();

                newUser.Name = model.Name;
                newUser.Email = model.Email;

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                userAuth = new UserAuth
                {
                    UserId = newUser.Id,
                    AuthKey = model.SignInType,
                    AuthValue = model.Id.ToString(),
                    IsCompleted = true
                    //IsCompleted = false
                };

                _context.UserAuth.Add(userAuth);
                await _context.SaveChangesAsync();

                response.Data = new IsCompletedResponseDto()
                {
                    id = model.Id.ToString(),
                    name= model.Name,
                    loginType = model.SignInType,
                    isCompleted = true
                    //isCompleted = false
                };
            }
            else
            {
                response.Data = new IsCompletedResponseDto()
                {
                    id = model.Id.ToString(),
                    name= model.Name,
                    loginType = model.SignInType,
                    isCompleted = userAuth.IsCompleted
                };
            }

            return response;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Data = new IsCompletedResponseDto();
            response.Code = MessageCode.Custom.UNKNOWN_ERROR.ToString();
            response.Message = MessageCode.CustomMessages[MessageCode.Custom.UNKNOWN_ERROR];

            _logService.LogError("EXCEPTION: IsCompleted", ex.Message, "ip: ");

            return response;
        }
    }

}