using System.Threading.Tasks;

namespace RepoMan
{
    public interface IWorker
    {
        public string Name { get; }
        Task ExecuteAsync();
    }
}