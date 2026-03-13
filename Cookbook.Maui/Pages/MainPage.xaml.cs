using Cookbook.Maui.Models;
using Cookbook.Maui.PageModels;

namespace Cookbook.Maui.Pages;

public partial class MainPage : ContentPage
{
	public MainPage(MainPageModel model)
	{
		InitializeComponent();
		BindingContext = model;
	}
}