using BIA.ToolKit.Application.Services;
using BIA.ToolKit.ViewModels;

namespace BIA.ToolKit.Application.Messages
{
    /// <summary>
    /// Message to request opening the repository form dialog
    /// </summary>
    public class OpenRepositoryFormRequestMessage
    {
        public RepositoryViewModel Repository { get; }
        public RepositoryFormMode Mode { get; }

        public OpenRepositoryFormRequestMessage(RepositoryViewModel repository, RepositoryFormMode mode)
        {
            Repository = repository;
            Mode = mode;
        }
    }
}
