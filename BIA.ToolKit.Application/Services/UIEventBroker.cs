﻿using System;
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

        public delegate void ProjectChanged(Project project);
        public delegate void NewVersionAvailable();
        public delegate void ActionWithWaiterAsync(Func<Task> action);

        public event ProjectChanged OnProjectChanged;
        public event NewVersionAvailable OnNewVersionAvailable;
        public event ActionWithWaiterAsync OnActionWithWaiter;

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
    }
}
