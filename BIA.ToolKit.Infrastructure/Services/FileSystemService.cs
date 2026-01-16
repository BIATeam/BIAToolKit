using BIA.ToolKit.Domain.Services;

namespace BIA.ToolKit.Infrastructure.Services
{
    /// <summary>
    /// Implementation of IFileSystemService wrapping System.IO operations
    /// </summary>
    public class FileSystemService : IFileSystemService
    {
        // Directory operations
        public bool DirectoryExists(string path) => Directory.Exists(path);

        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);

        public string[] GetDirectories(string path) => Directory.GetDirectories(path);

        public string[] GetDirectories(string path, string searchPattern) 
            => Directory.GetDirectories(path, searchPattern);

        public string[] GetDirectories(string path, string searchPattern, SearchOption searchOption) 
            => Directory.GetDirectories(path, searchPattern, searchOption);

        // File operations
        public bool FileExists(string path) => File.Exists(path);

        public void DeleteFile(string path) => File.Delete(path);

        public void CopyFile(string sourceFileName, string destFileName, bool overwrite) 
            => File.Copy(sourceFileName, destFileName, overwrite);

        public void MoveFile(string sourceFileName, string destFileName) 
            => File.Move(sourceFileName, destFileName);

        public string ReadAllText(string path) => File.ReadAllText(path);

        public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);

        public string[] ReadAllLines(string path) => File.ReadAllLines(path);

        public void WriteAllLines(string path, string[] contents) => File.WriteAllLines(path, contents);

        public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

        public void WriteAllBytes(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);

        public string[] GetFiles(string path) => Directory.GetFiles(path);

        public string[] GetFiles(string path, string searchPattern) 
            => Directory.GetFiles(path, searchPattern);

        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) 
            => Directory.GetFiles(path, searchPattern, searchOption);

        // Path operations
        public string GetFileName(string path) => Path.GetFileName(path);

        public string GetFileNameWithoutExtension(string path) => Path.GetFileNameWithoutExtension(path);

        public string GetDirectoryName(string path) => Path.GetDirectoryName(path) ?? string.Empty;

        public string GetExtension(string path) => Path.GetExtension(path);

        public string Combine(params string[] paths) => Path.Combine(paths);

        public string GetFullPath(string path) => Path.GetFullPath(path);
    }
}
