using System.Collections.Generic;
using System.Threading.Tasks;
using RepoMan.Records;
using RepoMan.Repository;

namespace RepoMan.IO
{
    public interface IPullRequestCacheManager
    {
        ValueTask SaveAsync(IList<PullRequestDetails> prDetails, string repoOwner, string repoName);
        ValueTask<IList<PullRequestDetails>> LoadAsync(string repoOwner, string repoName);
    }
}