using CalculatriceMaui.Models;
using CommunityToolkit.Mvvm.Input;

namespace CalculatriceMaui.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}