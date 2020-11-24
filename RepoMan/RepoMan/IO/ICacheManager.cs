using System.Collections.Generic;
using System.Threading.Tasks;
using RepoMan.PullRequest;

namespace RepoMan.IO
{
    public interface ICacheManager
    {
        ValueTask SaveAsync(IList<PullRequestDetails> prDetails, string repoOwner, string repoName);
        ValueTask<IList<PullRequestDetails>> LoadAsync(string repoOwner, string repoName);
    }
}