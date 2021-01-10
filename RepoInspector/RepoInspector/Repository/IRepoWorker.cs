using System.Threading.Tasks;

namespace RepoInspector.Repository
{
    public interface IRepoWorker
    {
        Task ExecuteAsync();
    }
}