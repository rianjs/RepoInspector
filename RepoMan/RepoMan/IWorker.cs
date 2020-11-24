using System.Threading.Tasks;

namespace RepoMan
{
    public interface IWorker
    {
        Task ExecuteAsync();
    }
}