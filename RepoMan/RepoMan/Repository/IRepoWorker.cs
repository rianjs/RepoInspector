using System.Threading.Tasks;

namespace RepoMan.Repository
{
    public interface IRepoWorker
    {
        Task ExecuteAsync();
    }
}