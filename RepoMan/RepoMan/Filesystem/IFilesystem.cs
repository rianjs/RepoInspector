using System.Threading.Tasks;

namespace RepoMan.Filesystem
{
    public interface IFilesystem
    {
        Task<string> FileReadAllTextAsync(string path);
        Task FileWriteAllTextAsync(string path, string contents);
    }
}