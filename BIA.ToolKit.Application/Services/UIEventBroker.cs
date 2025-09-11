using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BIA.ToolKit.Domain;
using BIA.ToolKit.Domain.ModifyProject;
using BIA.ToolKit.Domain.Settings;

namespace BIA.ToolKit.Application.Services
{
    public class UIEventBroker
    {
        private readonly SettingsService settingsService;

        public enum TabItemModifyProjectEnum
        {
            Migration,
            OptionGenerator,
            DtoGenerator,
            CrudGenerator
        }

        public delegate void ProjectChanged(Project project);
        public delegate void NewVersionAvailable();
        public delegate void ActionWithWaiterAsync(Func<Task> action);
        public delegate void SettingsUpdated(IBIATKSettings settings);
        public delegate void RepositoryChanged(IRepository repository);

        public event ProjectChanged OnProjectChanged;
        public event NewVersionAvailable OnNewVersionAvailable;
        public event ActionWithWaiterAsync OnActionWithWaiter;
        public event SettingsUpdated OnSettingsUpdated;
        public event RepositoryChanged OnRepositoryChanged;

        public void NotifyProjectChanged(Project project)
        {
            OnProjectChanged?.Invoke(project);
        }

        public void NotifyNewVersionAvailable()
        {
            OnNewVersionAvailable?.Invoke();
        }

        public void ExecuteActionWithWaiter(Func<Task> task)
        {
            OnActionWithWaiter?.Invoke(task);
        }

        public void NotifySettingsUpdated(IBIATKSettings settings)
        {
            OnSettingsUpdated?.Invoke(settings);
        }

        public void NotifyRepositoryChanged(IRepository repository)
        { 
            OnRepositoryChanged?.Invoke(repository); 
        }
    }
}
