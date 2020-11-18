using System.Collections.Generic;
using System.Threading.Tasks;

namespace RepoMan.RepoHistory
{
    public interface IRepoHistoryManager
    {
        /// <summary>
        /// Sometimes it's not possible to fully interrogate the commit graph
        /// </summary>
        /// <returns></returns>
        Task<ICollection<PullRequestDetails>> GetUnfinishedPullRequestsAsync();

        /// <summary>
        /// Async-await and thread-safe update for recently-imported pull requests. Persists the updated collection to its home.
        /// </summary>
        /// <param name="imports"></param>
        /// <returns></returns>
        Task ImportPullRequestsAsync(ICollection<PullRequestDetails> imports);

        /// <summary>
        /// Attempts to fill out the missing data associated with unfinished pull requests, in a serial manner so as to avoid pounding the underlying API.
        /// </summary>
        /// <param name="unfinishedPrs"></param>
        /// <param name="prReader"></param>
        /// <param name="delay">Delay to wait before trying to update the next pull request in the list</param>
        /// <returns></returns>
        Task PopulateCommentsAndApprovalsAsync();

        /// <summary>
        /// Returns the number of pull requests in the cache that have been fully populated
        /// </summary>
        /// <returns></returns>
        Task<int> GetPullRequestCount();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prNumber"></param>
        /// <returns>null if the pull request number is not present</returns>
        Task<PullRequestDetails> GetPullRequestByNumber(int prNumber);

        IAsyncEnumerable<PullRequestDetails> GetPullRequestsAsync();

        /// <summary>
        /// Returns the comments on each PR
        /// </summary>
        /// <returns></returns>
        Task<IList<Comment>> GetAllCommentsForRepo();
    }
}