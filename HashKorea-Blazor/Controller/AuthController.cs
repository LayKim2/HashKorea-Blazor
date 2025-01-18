using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalyxManagement.Controller
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpGet("signin/kakao")]
        public IActionResult SigninKakao()
        {
            // Generate a unique state
            var state = Guid.NewGuid().ToString("N");

            // Store the state in the session or memory cache
            HttpContext.Session.SetString("KakaoState", state);

            var properties = new AuthenticationProperties
            {
                RedirectUri = "/api/auth/signin/kakao/callback",
                Items =
                {
                    { "state", state } // Store state in the AuthenticationProperties
                }
                    
            };

            return Challenge(properties, "KakaoTalk");
        }

        [HttpGet("signin/kakao/callback")]
        public async Task<IActionResult> SigninKakaoCallback(string code, string state)
        {
            var storedState = HttpContext.Session.GetString("KakaoState");

            // 로그 확인
            Console.WriteLine("signin-kakao-callback");

            var result = await HttpContext.AuthenticateAsync("KakaoTalk");

            if (result?.Succeeded != true)
            {
                return Redirect("/");
            }

            Console.WriteLine("signin-kakao-callback 2");

            return Redirect("/");
        }
    }
}
