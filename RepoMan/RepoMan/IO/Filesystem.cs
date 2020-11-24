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
    }
}