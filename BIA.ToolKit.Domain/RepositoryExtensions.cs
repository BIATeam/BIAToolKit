namespace BIA.ToolKit.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class RepositoryExtensions
    {
        public static IRepository ToRepository(this RepositoryUserConfig repositoryUserConfig)
        {
            return repositoryUserConfig.RepositoryType switch
            {
                RepositoryType.Git => repositoryUserConfig.ReleaseType switch
                {
                    ReleaseType.Git => RepositoryGit.CreateWithReleaseTypeGit(
                        name: repositoryUserConfig.Name,
                        repositoryGitKind: repositoryUserConfig.RepositoryGitKind,
                        url: repositoryUserConfig.Url,
                        urlRelease: repositoryUserConfig.UrlRelease,
                        gitRepositoryName: repositoryUserConfig.GitRepositoryName,
                        owner: repositoryUserConfig.Owner,
                        useLocalClonedFolder: repositoryUserConfig.UseLocalClonedFolder,
                        companyName: repositoryUserConfig.CompanyName,
                        projectName: repositoryUserConfig.ProjectName,
                        localClonedFolderPath: repositoryUserConfig.LocalClonedFolderPath,
                        useRepository: repositoryUserConfig.UseRepository
                    ),
                    ReleaseType.Folder => RepositoryGit.CreateWithReleaseTypeFolder(
                        name: repositoryUserConfig.Name,
                        url: repositoryUserConfig.Url,
                        useLocalClonedFolder: repositoryUserConfig.UseLocalClonedFolder,
                        releasesFolderRegexPattern: repositoryUserConfig.ReleasesFolderRegexPattern,
                        companyName: repositoryUserConfig.CompanyName,
                        projectName: repositoryUserConfig.ProjectName,
                        localClonedFolderPath: repositoryUserConfig.LocalClonedFolderPath,
                        useRepository: repositoryUserConfig.UseRepository
                    ),
                    ReleaseType.Tag => RepositoryGit.CreateWithReleaseTypeTag(
                        name: repositoryUserConfig.Name,
                        url: repositoryUserConfig.Url,
                        useLocalClonedFolder: repositoryUserConfig.UseLocalClonedFolder,
                        companyName: repositoryUserConfig.CompanyName,
                        projectName: repositoryUserConfig.ProjectName,
                        localClonedFolderPath: repositoryUserConfig.LocalClonedFolderPath,
                        useRepository: repositoryUserConfig.UseRepository
                    ),
                    _ => throw new NotImplementedException(),
                },
                RepositoryType.Folder => new RepositoryFolder(
                    name: repositoryUserConfig.Name,
                    path: repositoryUserConfig.Path,
                    releasesFolderRegexPattern: repositoryUserConfig.ReleasesFolderRegexPattern,
                    companyName: repositoryUserConfig.CompanyName,
                    projectName: repositoryUserConfig.ProjectName,
                    useRepository: repositoryUserConfig.UseRepository),
                _ => throw new NotImplementedException(),
            };
        }
    }
}
