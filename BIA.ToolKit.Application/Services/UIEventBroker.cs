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
    public enum RepositoryFormMode
    {
        Create,
        Edit
    }

    public class UIEventBroker
    {
        public delegate void ProjectChanged(Project project);
        public delegate void NewVersionAvailable();
        public delegate void ExecuteActionWithWaiterAsyncRequest(Func<Task> action);
        public delegate void SettingsUpdated(IBIATKSettings settings);
        public delegate void RepositoriesUpdated();
        public delegate void RepositoryViewModelChanged(RepositoryViewModel oldRepository, RepositoryViewModel newRepository);
        public delegate void RepositoryViewModelDeleted(RepositoryViewModel repository);
        public delegate void OpenRepositoryFormRequest(RepositoryViewModel repository, RepositoryFormMode mode);
        public delegate void RepositoryViewModelAdded(RepositoryViewModel repository);
        public delegate void RepositoryViewModelVersionXYZChanged(RepositoryViewModel repository);
        public delegate void SolutionLoaded();

        public event ProjectChanged OnProjectChanged;
        public event NewVersionAvailable OnNewVersionAvailable;
        public event ExecuteActionWithWaiterAsyncRequest OnExecuteActionWithWaiterAsyncRequest;
        public event SettingsUpdated OnSettingsUpdated;
        public event RepositoryViewModelChanged OnRepositoryViewModelChanged;
        public event OpenRepositoryFormRequest OnOpenRepositoryFormRequest;
        public event RepositoriesUpdated OnRepositoriesUpdated;
        public event RepositoryViewModelDeleted OnRepositoryViewModelDeleted;
        public event RepositoryViewModelAdded OnRepositoryViewModelAdded;
        public event RepositoryViewModelVersionXYZChanged OnRepositoryViewModelVersionXYZChanged;
        public event SolutionLoaded OnSolutionLoaded;

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

        public void RequestOpenRepositoryForm(RepositoryViewModel repository, RepositoryFormMode mode)
        {
            OnOpenRepositoryFormRequest?.Invoke(repository, mode);
        }

        public void NotifyRepositoryViewModelDeleted(RepositoryViewModel repository)
        {
            OnRepositoryViewModelDeleted?.Invoke(repository);
        }

        public void NotifyRepositoryViewModelAdded(RepositoryViewModel repository)
        {
            OnRepositoryViewModelAdded?.Invoke(repository);
        }

        public void NotifyViewModelVersionXYZChanged(RepositoryViewModel repository)
        {
            OnRepositoryViewModelVersionXYZChanged?.Invoke(repository);
        }

        public void NotifySolutionLoaded()
        {
            OnSolutionLoaded?.Invoke();
        }
    }
}
