using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BIA.ToolKit.Domain.ModifyProject;

namespace BIA.ToolKit.Application.Services
{
    public class UIEventBroker
    {
        public enum TabItemModifyProjectEnum
        {
            Migration,
            OptionGenerator,
            DtoGenerator,
            CrudGenerator
        }

        public delegate void ProjectChanged(Project project, TabItemModifyProjectEnum currentTabItem);
        public delegate void NewVersionAvailable();

        public event ProjectChanged OnProjectChanged;
        public event NewVersionAvailable OnNewVersionAvailable;

        public TabItemModifyProjectEnum CurrentTabItemModifyProject { get; private set; }

        public void NotifyProjectChanged(Project project)
        {
            OnProjectChanged?.Invoke(project, CurrentTabItemModifyProject);
        }

        public void SetCurrentTabItemModifyProject(TabItemModifyProjectEnum tabItem)
        {
            CurrentTabItemModifyProject = tabItem;
        }

        public void NotifyNewVersionAvailable()
        {
            OnNewVersionAvailable?.Invoke();
        }
    }
}
