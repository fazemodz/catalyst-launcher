using System.Net.Http;
using System.Net.Http.Json;

namespace Catalyst_Launcher.Services;

public enum LoginResult
{
    Success,
    InvalidCredentials,
    UserExists,
    NetworkError,
    EmptyFields
}

public record AuthUserInfo(string Username, string Email, string Token);

public static class LoginService
{
    // TODO: Replace with real API base URL.
    private const string ApiBaseUrl = "https://api.example.com";

    public static async Task<(LoginResult Result, AuthUserInfo? User)> LoginAsync(
        string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (LoginResult.EmptyFields, null);
        await Task.Delay(700); 
        return (LoginResult.Success, new AuthUserInfo(email.Split('@')[0], email, "placeholder-token"));
    }

    public static async Task<(LoginResult Result, AuthUserInfo? User)> RegisterAsync(
        string username, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
            return (LoginResult.EmptyFields, null);
        ;

        await Task.Delay(700);
        return (LoginResult.Success, new AuthUserInfo(username, email, "placeholder-token"));
    }
}
