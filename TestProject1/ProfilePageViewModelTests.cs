using CookbookMauiBlazor.Shared.Services;
using CookbookMauiBlazor.Shared.Viewmodels;
using Microsoft.Extensions.Configuration;

namespace TestProject1;

public class ProfilePageViewModelTests
{
    private readonly Mock<IUserProfileService> _mockProfileService = new();

    private static IConfiguration MockConfig(bool profilesEnabled)
    {
        var section = new Mock<IConfigurationSection>();
        section.Setup(s => s.Value).Returns(profilesEnabled ? "true" : "false");
        var config = new Mock<IConfiguration>();
        config.Setup(c => c.GetSection("FeatureFlags:EnableUserProfiles")).Returns(section.Object);
        return config.Object;
    }

    private static ProfileStateService CreateProfileStateService() => new();

    [Fact]
    public async Task InitializeAsync_WhenProfilesDisabled_SetsProfilesEnabledFalseAndStopsLoading()
    {
        var vm = new ProfilePageViewModel(
            FakeAuthStateProvider.Authenticated("user1"),
            _mockProfileService.Object,
            MockConfig(false),
            CreateProfileStateService());

        await vm.InitializeAsync();

        Assert.False(vm.ProfilesEnabled);
        Assert.False(vm.Loading);
        _mockProfileService.Verify(s => s.GetProfileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_WhenProfileExists_PopulatesProfileModelAndPictureUrl()
    {
        var profile = new UserProfileDto("user1", "John", "Doe", "JD", "http://pic.com/1.jpg");
        _mockProfileService.Setup(s => s.GetProfileAsync("user1", default)).ReturnsAsync(profile);
        var vm = new ProfilePageViewModel(
            FakeAuthStateProvider.Authenticated("user1"),
            _mockProfileService.Object,
            MockConfig(true),
            CreateProfileStateService());

        await vm.InitializeAsync();

        Assert.Equal("John", vm.ProfileModel.FirstName);
        Assert.Equal("Doe", vm.ProfileModel.LastName);
        Assert.Equal("JD", vm.ProfileModel.DisplayName);
        Assert.Equal("http://pic.com/1.jpg", vm.PictureUrl);
        Assert.False(vm.Loading);
    }

    [Fact]
    public async Task InitializeAsync_WhenProfileNotFound_LeavesModelEmpty()
    {
        _mockProfileService.Setup(s => s.GetProfileAsync("user1", default)).ReturnsAsync((UserProfileDto?)null);
        var vm = new ProfilePageViewModel(
            FakeAuthStateProvider.Authenticated("user1"),
            _mockProfileService.Object,
            MockConfig(true),
            CreateProfileStateService());

        await vm.InitializeAsync();

        Assert.Equal(string.Empty, vm.ProfileModel.FirstName);
        Assert.Null(vm.PictureUrl);
        Assert.False(vm.Loading);
    }

    [Fact]
    public async Task SaveProfileAsync_OnSuccess_SetsSaveSuccess()
    {
        var profile = new UserProfileDto("user1", "Jane", "Smith", "JS", null);
        _mockProfileService.Setup(s => s.GetProfileAsync("user1", default)).ReturnsAsync((UserProfileDto?)null);
        _mockProfileService.Setup(s => s.UpdateProfileAsync("user1", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(profile);
        var vm = new ProfilePageViewModel(
            FakeAuthStateProvider.Authenticated("user1"),
            _mockProfileService.Object,
            MockConfig(true),
            CreateProfileStateService());
        await vm.InitializeAsync();

        await vm.SaveProfileAsync();

        Assert.True(vm.SaveSuccess);
        Assert.Null(vm.SaveError);
        Assert.False(vm.Saving);
    }

    [Fact]
    public async Task SaveProfileAsync_OnFailure_SetsSaveError()
    {
        _mockProfileService.Setup(s => s.GetProfileAsync("user1", default)).ReturnsAsync((UserProfileDto?)null);
        _mockProfileService.Setup(s => s.UpdateProfileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync((UserProfileDto?)null);
        var vm = new ProfilePageViewModel(
            FakeAuthStateProvider.Authenticated("user1"),
            _mockProfileService.Object,
            MockConfig(true),
            CreateProfileStateService());
        await vm.InitializeAsync();

        await vm.SaveProfileAsync();

        Assert.False(vm.SaveSuccess);
        Assert.Equal("Failed to save. Please try again.", vm.SaveError);
        Assert.False(vm.Saving);
    }
}
