using System.Collections.Generic;
using System.Threading.Tasks;
using RepoMan;
using RepoMan.Records;
using RepoMan.Repository;

namespace Scratch
{
    public class BitbucketPullRequestReader :
        IRepoPullRequestReader
    {
        public Task<IList<PullRequest>> GetPullRequestsRootAsync(ItemState stateFilter)
        {
            throw new System.NotImplementedException();
        }

        public Task<bool> TryFillCommentGraphAsync(PullRequest pullRequest)
        {
            throw new System.NotImplementedException();
        }
    }
}