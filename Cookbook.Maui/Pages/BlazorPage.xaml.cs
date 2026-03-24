namespace Cookbook.Maui.Pages;

public partial class BlazorPage : ContentPage
{
	public BlazorPage()
	{
		InitializeComponent();
	}

	protected override async void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);

		// Ensure the blazor web view is properly initialized when the page is navigated to
		if (blazorWebView != null)
		{
			await Task.Delay(100); // Small delay to ensure proper initialization
		}
	}
}
