using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace TestProject1;

internal sealed class FakeAuthStateProvider : AuthenticationStateProvider
{
    private readonly AuthenticationState _state;

    public FakeAuthStateProvider(ClaimsPrincipal user)
        => _state = new AuthenticationState(user);

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(_state);

    public static FakeAuthStateProvider Authenticated(string userId)
    {
        var identity = new ClaimsIdentity([new Claim("oid", userId)], "TestAuth");
        return new FakeAuthStateProvider(new ClaimsPrincipal(identity));
    }

    public static FakeAuthStateProvider Anonymous()
        => new(new ClaimsPrincipal(new ClaimsIdentity()));
}

internal sealed class TestNavigationManager : NavigationManager
{
    public string? LastNavigatedUri { get; private set; }

    public TestNavigationManager()
    {
        Initialize("http://localhost/", "http://localhost/");
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        LastNavigatedUri = uri;
    }
}
