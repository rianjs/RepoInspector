using System.Collections.Generic;
using System.Threading.Tasks;
using RepoInspector.Records;
using PullRequest = RepoInspector.Records.PullRequest;

namespace RepoInspector.Repository
{
    public interface IRepoManager
    {
        string RepoOwner { get; }
        string RepoName { get; }
        string RepoUrl { get; }
        
        /// <summary>
        /// Check the upstream git repo API for any pull requests that the cache manager doesn't know about. For any that are found, do a deep query to get all
        /// of the approvals, comments, and other information required to construct a complete record of the pull request's details.
        /// </summary>
        /// <param name="stateFilter"></param>
        /// <returns>A collection of all of the new or updated pull requests that were unknown to or had to be updated in the manager</returns>
        Task<IList<PullRequest>> RefreshFromUpstreamAsync(ItemState stateFilter);
        
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

        ValueTask<IList<PullRequest>> GetPullRequestsByNumber(IEnumerable<int> prNumbers);

        ValueTask<IList<PullRequest>> GetPullRequestsAsync();

        /// <summary>
        /// Returns the comments on each PR
        /// </summary>
        /// <returns></returns>
        ValueTask<IList<Comment>> GetAllCommentsForRepo();
    }
}