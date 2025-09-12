using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BIA.ToolKit.Application.ViewModel;
using BIA.ToolKit.Domain;
using BIA.ToolKit.Domain.ModifyProject;
using BIA.ToolKit.Domain.Settings;

namespace BIA.ToolKit.Application.Services
{
    public class UIEventBroker
    {
        public delegate void ProjectChanged(Project project);
        public delegate void NewVersionAvailable();
        public delegate void ExecuteActionWithWaiterAsyncRequest(Func<Task> action);
        public delegate void SettingsUpdated(IBIATKSettings settings);
        public delegate void RepositoriesUpdated();
        public delegate void RepositoryViewModelChanged(RepositoryViewModel oldRepository, RepositoryViewModel newRepository);
        public delegate void OpenRepositoryFormRequest(RepositoryViewModel repository);

        public event ProjectChanged OnProjectChanged;
        public event NewVersionAvailable OnNewVersionAvailable;
        public event ExecuteActionWithWaiterAsyncRequest OnExecuteActionWithWaiterAsyncRequest;
        public event SettingsUpdated OnSettingsUpdated;
        public event RepositoryViewModelChanged OnRepositoryViewModelChanged;
        public event OpenRepositoryFormRequest OnOpenRepositoryFormRequest;
        public event RepositoriesUpdated OnRepositoriesUpdated;

        public void NotifyProjectChanged(Project project)
        {
            OnProjectChanged?.Invoke(project);
        }

        public void NotifyNewVersionAvailable()
        {
            OnNewVersionAvailable?.Invoke();
        }

        public void RequestExecuteActionWithWaiter(Func<Task> task)
        {
            OnExecuteActionWithWaiterAsyncRequest?.Invoke(task);
        }

        public void NotifySettingsUpdated(IBIATKSettings settings)
        {
            OnSettingsUpdated?.Invoke(settings);
        }

        public void NotifyRepositoriesUpdated()
        {
            OnRepositoriesUpdated?.Invoke();
        }

        public void NotifyRepositoryViewModelChanged(RepositoryViewModel oldRepository, RepositoryViewModel newRepository)
        { 
            OnRepositoryViewModelChanged?.Invoke(oldRepository, newRepository); 
        }

        public void RequestOpenRepositoryForm(RepositoryViewModel repository)
        {
            OnOpenRepositoryFormRequest?.Invoke(repository);
        }
    }
}
