namespace BIA.ToolKit.Infrastructure
{
    using BIA.ToolKit.ViewModels;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// ViewModelLocator for automatic ViewModel resolution via XAML
    /// Provides static access to ViewModels through DI container
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Gets LogDetailViewModel instance from DI container
        /// </summary>
        public LogDetailViewModel LogDetailViewModel => App.GetService<LogDetailViewModel>();

        // Future ViewModels will be added here as we migrate them
        // public RepositoryResumeViewModel RepositoryResumeViewModel => App.GetService<RepositoryResumeViewModel>();
        // public VersionAndOptionViewModel VersionAndOptionViewModel => App.GetService<VersionAndOptionViewModel>();
        // etc.
    }
}
