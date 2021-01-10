using System.Threading.Tasks;
using RepoInspector.Records;

namespace RepoInspector.Repository
{
    public interface IRepoManagerFactory
    {
        Task<IRepoManager> GetManagerAsync(WatchedRepository repo, bool refreshFromUpstream);
    }
}