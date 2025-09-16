namespace BIA.ToolKit.Domain
{
    using System.Threading.Tasks;

    public interface IRepositoryFolder : IRepository
    {
        string Path { get; }
        string ReleasesFolderRegexPattern { get; }
    }
}