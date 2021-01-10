using System.Threading.Tasks;

namespace RepoInspector
{
    public interface IWorker
    {
        public string Name { get; }
        Task ExecuteAsync();
    }
}