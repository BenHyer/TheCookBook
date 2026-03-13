using CommunityToolkit.Mvvm.Input;
using Cookbook.Maui.Models;

namespace Cookbook.Maui.PageModels;

public interface IProjectTaskPageModel
{
	IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
	bool IsBusy { get; }
}