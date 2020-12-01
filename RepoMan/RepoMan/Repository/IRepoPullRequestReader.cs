using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;

namespace RepoMan.Repository
{
    public interface IRepoPullRequestReader
    {
        /// <summary>
        /// Returns all of the closed Pull Requests associated with the repository. Makes no distinction between merged and unmerged.
        /// </summary>
        /// <param name="stateFilter"></param>
        /// <returns></returns>
        Task<IList<PullRequestDetails>> GetPullRequestsRootAsync(ItemStateFilter stateFilter);

        /// <summary>
        /// Fills out the comments on the pull request by doing concurrent calls to the various GitHub comment APIs, and aggregating the results
        /// </summary>
        /// <param name="pullRequest"></param>
        /// <returns></returns>
        Task<bool> TryFillCommentGraphAsync(PullRequestDetails pullRequest);
    }
}