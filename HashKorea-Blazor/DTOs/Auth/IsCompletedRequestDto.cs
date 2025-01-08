namespace HashKorea.DTOs.Auth;

public class IsCompletedRequestDto
{
    public string SignInType { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}