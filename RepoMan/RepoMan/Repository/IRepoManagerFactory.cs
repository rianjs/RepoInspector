using System.Threading.Tasks;
using RepoMan.Records;

namespace RepoMan.Repository
{
    public interface IRepoManagerFactory
    {
        Task<IRepoManager> GetManagerAsync(WatchedRepository repo, bool refreshFromUpstream);
    }
}