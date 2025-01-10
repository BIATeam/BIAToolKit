using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BIA.ToolKit.Domain.ModifyProject;

namespace BIA.ToolKit.Services
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
        public event ProjectChanged OnProjectChanged;

        public delegate void BIAFrontFolderChanged();
        public event BIAFrontFolderChanged OnBIAFrontFolderChanged;
        public TabItemModifyProjectEnum CurrentTabItemModifyProject { get; private set; }

        public void NotifyProjectChanged(Project project)
        {
            OnProjectChanged?.Invoke(project, CurrentTabItemModifyProject);
        }

        public void NotifyBIAFrontFolderChanged()
        {
            OnBIAFrontFolderChanged?.Invoke();
        }

        public void SetCurrentTabItemModifyProject(TabItemModifyProjectEnum tabItem)
        {
            CurrentTabItemModifyProject = tabItem;
        }
    }
}
