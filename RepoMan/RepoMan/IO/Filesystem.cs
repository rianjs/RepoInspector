using System.IO;
using System.Threading.Tasks;

namespace RepoMan.IO
{
    public class Filesystem : IFilesystem
    {
        public Task<string> FileReadAllTextAsync(string path)
            => File.ReadAllTextAsync(path);

        public Task FileWriteAllTextAsync(string path, string contents)
            => File.WriteAllTextAsync(path, contents);

        public string[] DirectoryGetFiles(string parent, string searchPattern)
            => Directory.GetFiles(parent, searchPattern);

        public DirectoryInfo DirectoryCreateDirectory(string path)
            => Directory.CreateDirectory(path);
    }
}