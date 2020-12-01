using System.Collections.Generic;
using System.Threading.Tasks;
using RepoMan.Records;

namespace RepoMan.IO
{
    public interface IPullRequestCacheManager
    {
        ValueTask SaveAsync(IList<PullRequest> prDetails, string repoOwner, string repoName);
        ValueTask<IList<PullRequest>> LoadAsync(string repoOwner, string repoName);
    }
}