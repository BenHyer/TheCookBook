using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CommunityToolkit.Mvvm.ComponentModel;
using CookbookMauiBlazor.Shared.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;

namespace CookbookMauiBlazor.Shared.Viewmodels;

public partial class ProfilePageViewModel : ObservableObject
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IUserProfileService _profileService;
    private readonly IConfiguration _configuration;
    private readonly ProfileStateService _profileState;
    private string _userId = string.Empty;

    [ObservableProperty]
    private bool profilesEnabled = true;

    [ObservableProperty]
    private bool loading = true;

    [ObservableProperty]
    private bool saving;

    [ObservableProperty]
    private bool saveSuccess;

    [ObservableProperty]
    private string? saveError;

    [ObservableProperty]
    private bool pictureUploading;

    [ObservableProperty]
    private string? pictureError;

    [ObservableProperty]
    private string? pictureUrl;

    public ProfileFormModel ProfileModel { get; } = new();

    public ProfilePageViewModel(
        AuthenticationStateProvider authStateProvider,
        IUserProfileService profileService,
        IConfiguration configuration,
        ProfileStateService profileState)
    {
        _authStateProvider = authStateProvider;
        _profileService = profileService;
        _configuration = configuration;
        _profileState = profileState;
    }

    public async Task InitializeAsync()
    {
        ProfilesEnabled = _configuration.GetValue("FeatureFlags:EnableUserProfiles", true);
        if (!ProfilesEnabled)
        {
            Loading = false;
            return;
        }

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        _userId =
            user.FindFirst("oid")?.Value
            ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? string.Empty;

        if (!string.IsNullOrEmpty(_userId))
        {
            var profile = await _profileService.GetProfileAsync(_userId);
            if (profile is not null)
            {
                ProfileModel.FirstName = profile.FirstName;
                ProfileModel.LastName = profile.LastName;
                ProfileModel.DisplayName = profile.DisplayName;
                PictureUrl = profile.ProfilePictureUrl;
                _profileState.SetProfile(PictureUrl, profile.DisplayName);
            }
        }

        Loading = false;
    }

    public async Task SaveProfileAsync()
    {
        Saving = true;
        SaveSuccess = false;
        SaveError = null;

        var result = await _profileService.UpdateProfileAsync(
            _userId,
            ProfileModel.FirstName,
            ProfileModel.LastName,
            ProfileModel.DisplayName);

        Saving = false;
        if (result is not null)
        {
            SaveSuccess = true;
            _profileState.SetProfile(PictureUrl, ProfileModel.DisplayName);
        }
        else
        {
            SaveError = "Failed to save. Please try again.";
        }
    }

    public async Task HandlePictureSelectedAsync(InputFileChangeEventArgs e)
    {
        PictureError = null;
        PictureUploading = true;

        var file = e.File;
        if (file.Size > 5 * 1024 * 1024)
        {
            PictureError = "File must be under 5 MB.";
            PictureUploading = false;
            return;
        }

        await using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024);
        var url = await _profileService.UploadProfilePictureAsync(
            _userId,
            stream,
            file.Name,
            file.ContentType);

        PictureUploading = false;
        if (url is not null)
        {
            PictureUrl = url;
            _profileState.SetProfile(url, ProfileModel.DisplayName);
        }
        else
        {
            PictureError = "Upload failed. Please try again.";
        }
    }

    public sealed class ProfileFormModel
    {
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;
    }
}
