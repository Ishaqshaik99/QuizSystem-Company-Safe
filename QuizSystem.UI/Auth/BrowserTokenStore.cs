using Microsoft.JSInterop;

namespace QuizSystem.UI.Auth;

public class BrowserTokenStore
{
    private const string AccessTokenKey = "quizsystem.access_token";
    private const string RefreshTokenKey = "quizsystem.refresh_token";

    private readonly IJSRuntime _jsRuntime;

    public BrowserTokenStore(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>("quizAuth.get", AccessTokenKey);
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        return await _jsRuntime.InvokeAsync<string?>("quizAuth.get", RefreshTokenKey);
    }

    public async Task SetTokensAsync(string accessToken, string refreshToken)
    {
        await _jsRuntime.InvokeVoidAsync("quizAuth.set", AccessTokenKey, accessToken);
        await _jsRuntime.InvokeVoidAsync("quizAuth.set", RefreshTokenKey, refreshToken);
    }

    public async Task ClearAsync()
    {
        await _jsRuntime.InvokeVoidAsync("quizAuth.remove", AccessTokenKey);
        await _jsRuntime.InvokeVoidAsync("quizAuth.remove", RefreshTokenKey);
    }
}
