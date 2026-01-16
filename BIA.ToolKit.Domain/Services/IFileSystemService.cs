namespace BIA.ToolKit.Domain.Services
{
    /// <summary>
    /// Abstraction for file system operations to enable testing and decouple from System.IO
    /// </summary>
    public interface IFileSystemService
    {
        // Directory operations
        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        void DeleteDirectory(string path, bool recursive);
        string[] GetDirectories(string path);
        string[] GetDirectories(string path, string searchPattern);
        string[] GetDirectories(string path, string searchPattern, SearchOption searchOption);

        // File operations
        bool FileExists(string path);
        void DeleteFile(string path);
        void CopyFile(string sourceFileName, string destFileName, bool overwrite);
        void MoveFile(string sourceFileName, string destFileName);
        string ReadAllText(string path);
        void WriteAllText(string path, string contents);
        string[] ReadAllLines(string path);
        void WriteAllLines(string path, string[] contents);
        byte[] ReadAllBytes(string path);
        void WriteAllBytes(string path, byte[] bytes);
        string[] GetFiles(string path);
        string[] GetFiles(string path, string searchPattern);
        string[] GetFiles(string path, string searchPattern, SearchOption searchOption);

        // Path operations
        string GetFileName(string path);
        string GetFileNameWithoutExtension(string path);
        string GetDirectoryName(string path);
        string GetExtension(string path);
        string Combine(params string[] paths);
        string GetFullPath(string path);
    }
}
