using System.Collections.Generic;
using System.Threading.Tasks;
using PullRequest = RepoMan.Records.PullRequest;

namespace RepoMan.Repository
{
    public interface IRepoPullRequestReader
    {
        /// <summary>
        /// Returns all of the closed Pull Requests associated with the repository. Makes no distinction between merged and unmerged.
        /// </summary>
        /// <param name="stateFilter"></param>
        /// <returns></returns>
        Task<IList<PullRequest>> GetPullRequestsRootAsync(ItemState stateFilter);

        /// <summary>
        /// Fills out the comments on the pull request by doing concurrent calls to the various GitHub comment APIs, and aggregating the results
        /// </summary>
        /// <param name="pullRequest"></param>
        /// <returns></returns>
        Task<bool> TryFillCommentGraphAsync(PullRequest pullRequest);
    }
}