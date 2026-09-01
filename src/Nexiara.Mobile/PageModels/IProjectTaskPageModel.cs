using CommunityToolkit.Mvvm.Input;
using Nexiara.Mobile.Models;

namespace Nexiara.Mobile.PageModels
{
    public interface IProjectTaskPageModel
    {
        IAsyncRelayCommand<ProjectTask> NavigateToTaskCommand { get; }
        bool IsBusy { get; }
    }
}