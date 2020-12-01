using System.IO;
using System.Threading.Tasks;

namespace RepoMan.IO
{
    public interface IFilesystem
    {
        Task<string> FileReadAllTextAsync(string path);
        Task FileWriteAllTextAsync(string path, string contents);

        DirectoryInfo DirectoryCreateDirectory(string path);
        string[] DirectoryGetFiles(string path, string searchPattern);
    }
}