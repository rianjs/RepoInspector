using System.IO;
using System.Threading.Tasks;

namespace RepoInspector.Runner.IO
{
    public interface IFilesystem
    {
        Task<string> FileReadAllTextAsync(string path);
        Task FileWriteAllTextAsync(string path, string contents);
        string[] DirectoryGetFiles(string parent, string searchPattern);
        DirectoryInfo DirectoryCreateDirectory(string path);
    }
}