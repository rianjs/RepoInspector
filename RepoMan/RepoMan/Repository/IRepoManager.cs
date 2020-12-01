using System.Collections.Generic;
using System.Threading.Tasks;
using Octokit;
using RepoMan.Records;
using PullRequest = RepoMan.Records.PullRequest;

namespace RepoMan.Repository
{
    public interface IRepoManager
    {
        string RepoOwner { get; }
        string RepoName { get; }
        
        /// <summary>
        /// Check the upstream git repo API for any pull requests that the cache manager doesn't know about. For any that are found, do a deep query to get all
        /// of the approvals, comments, and other information required to construct a complete record of the pull request's details.
        /// </summary>
        /// <param name="stateFilter"></param>
        /// <returns>A collection of all of the new or updated pull requests that were unknown to or had to be updated in the manager</returns>
        Task<IList<PullRequest>> RefreshFromUpstreamAsync(ItemStateFilter stateFilter);
        
        /// <summary>
        /// Returns the number of pull requests in the cache that have been fully populated
        /// </summary>
        /// <returns></returns>
        ValueTask<int> GetPullRequestCount();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prNumber"></param>
        /// <returns>null if the pull request number is not present</returns>
        ValueTask<PullRequest> GetPullRequestByNumber(int prNumber);

        /// <summary>
        /// Returns the cached pull requests
        /// </summary>
        /// <returns></returns>
        ValueTask<IList<PullRequest>> GetCachedPullRequestsAsync();

        /// <summary>
        /// Returns the comments on each PR
        /// </summary>
        /// <returns></returns>
        ValueTask<IList<Comment>> GetAllCommentsForRepo();
    }
}