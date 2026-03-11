using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace QuizSystem.UI.Auth;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly BrowserTokenStore _tokenStore;
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public JwtAuthenticationStateProvider(BrowserTokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokenStore.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            return new AuthenticationState(_anonymous);
        }

        var claims = ParseClaims(token);
        if (claims.Count == 0)
        {
            return new AuthenticationState(_anonymous);
        }

        var identity = new ClaimsIdentity(claims, "jwtAuth");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task MarkUserAsAuthenticatedAsync(string accessToken)
    {
        var claims = ParseClaims(accessToken);
        var identity = new ClaimsIdentity(claims, "jwtAuth");
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity))));
        await Task.CompletedTask;
    }

    public void MarkUserAsLoggedOut()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }

    private static List<Claim> ParseClaims(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(jwt))
        {
            return new List<Claim>();
        }

        var token = handler.ReadJwtToken(jwt);
        var claims = token.Claims.ToList();

        var sub = claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        if (!string.IsNullOrWhiteSpace(sub) && claims.All(c => c.Type != ClaimTypes.NameIdentifier))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, sub));
        }

        return claims;
    }
}
