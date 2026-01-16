using System;
using System.Threading.Tasks;

namespace BIA.ToolKit.Application.Messages
{
    /// <summary>
    /// Message to request execution of an async action with waiter UI
    /// </summary>
    public class ExecuteActionWithWaiterMessage
    {
        public Func<Task> Action { get; }

        public ExecuteActionWithWaiterMessage(Func<Task> action)
        {
            Action = action;
        }
    }
}
