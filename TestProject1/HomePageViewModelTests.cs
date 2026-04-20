using CookbookMauiBlazor.Shared.Services;
using CookbookMauiBlazor.Shared.Viewmodels;

namespace TestProject1;

public class HomePageViewModelTests
{
    [Fact]
    public async Task InitializeAsync_WhenAuthenticated_SetsRequiresSignInFalse()
    {
        var vm = new HomePageViewModel(FakeAuthStateProvider.Authenticated("user1"), new Mock<IAuthService>().Object);

        await vm.InitializeAsync();

        Assert.False(vm.RequiresSignIn);
    }

    [Fact]
    public async Task InitializeAsync_WhenUnauthenticated_SetsRequiresSignInTrue()
    {
        var vm = new HomePageViewModel(FakeAuthStateProvider.Anonymous(), new Mock<IAuthService>().Object);

        await vm.InitializeAsync();

        Assert.True(vm.RequiresSignIn);
    }

    [Fact]
    public async Task SignInAsync_DelegatesToAuthService()
    {
        var mockAuth = new Mock<IAuthService>();
        var vm = new HomePageViewModel(FakeAuthStateProvider.Anonymous(), mockAuth.Object);

        await vm.SignInAsync();

        mockAuth.Verify(a => a.SignInAsync("/"), Times.Once);
    }
}
