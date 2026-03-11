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
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("quizAuth.get", AccessTokenKey);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("quizAuth.get", RefreshTokenKey);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
    }

    public async Task SetTokensAsync(string accessToken, string refreshToken)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("quizAuth.set", AccessTokenKey, accessToken);
            await _jsRuntime.InvokeVoidAsync("quizAuth.set", RefreshTokenKey, refreshToken);
        }
        catch (InvalidOperationException)
        {
        }
        catch (JSDisconnectedException)
        {
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("quizAuth.remove", AccessTokenKey);
            await _jsRuntime.InvokeVoidAsync("quizAuth.remove", RefreshTokenKey);
        }
        catch (InvalidOperationException)
        {
        }
        catch (JSDisconnectedException)
        {
        }
    }
}
