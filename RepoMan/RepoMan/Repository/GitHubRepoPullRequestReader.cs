using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;

namespace RepoMan.Repository
{
    /// <summary>
    /// Represents a GitHub pull request reader intended to be used against a specific repository.
    /// </summary>
    public class GitHubRepoPullRequestReader :
        IRepoPullRequestReader
    {
        private readonly string _repoOwner;
        private readonly string _repoName;
        private readonly GitHubClient _client;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="repoOwner"></param>
        /// <param name="repoName"></param>
        /// <param name="client">This client instance may be used across GitHubRepoPullRequestReader instances</param>
        public GitHubRepoPullRequestReader(string repoOwner, string repoName, GitHubClient client)
        {
            _repoOwner = string.IsNullOrWhiteSpace(repoOwner)
                ? throw new ArgumentNullException(nameof(repoOwner))
                : repoOwner;
            
            _repoName = string.IsNullOrWhiteSpace(repoName)
                ? throw new ArgumentNullException(nameof(repoName))
                : repoName;

            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Returns all of the closed Pull Requests associated with the repository. Makes no distinction between merged and unmerged.
        /// </summary>
        /// <param name="repoOwner"></param>
        /// <param name="repoName"></param>
        /// <param name="stateFilter"></param>
        /// <returns></returns>
        public async Task<IList<PullRequestDetails>> GetPullRequestsRootAsync(ItemStateFilter stateFilter)
        {
            var prOpts = new PullRequestRequest
            {
                State = stateFilter,
                SortProperty = PullRequestSort.Created,
                SortDirection = SortDirection.Ascending,
            };
            var pullRequests = await _client.PullRequest.GetAllForRepository(_repoOwner, _repoName, prOpts);

            var reduced = pullRequests
                .AsParallel()
                .Select(pr => new PullRequestDetails(pr))
                .ToList();

            return reduced;
        }
        
        /// <summary>
        /// Fills out the comments on the pull request by doing concurrent calls to the various GitHub comment APIs, and aggregating the results
        /// </summary>
        /// <param name="pullRequest"></param>
        /// <returns></returns>
        public async Task<bool> TryFillCommentGraphAsync(PullRequestDetails pullRequest)
        {
            // Comments on specific lines and ranges of lines in the changed code
            var diffReviewCommentsTask = _client.PullRequest.ReviewComment.GetAll(_repoOwner, _repoName, pullRequest.Number);

            // State transitions (APPROVED), and comments associated with them
            var approvalSummariesTask = _client.PullRequest.Review.GetAll(_repoOwner, _repoName, pullRequest.Number);

            // These are the comments on the PR in general, not associated with an approval, or with a commit, or with something in the diff
            var generalPrCommentsTask = _client.Issue.Comment.GetAllForIssue(_repoOwner, _repoName, pullRequest.Number);
            
            await Task.WhenAll(diffReviewCommentsTask, approvalSummariesTask, generalPrCommentsTask);
            
            if (diffReviewCommentsTask.IsFaulted || generalPrCommentsTask.IsFaulted || approvalSummariesTask.IsFaulted)
            {
                return false;
            }

            pullRequest.UpdateDiffComments(diffReviewCommentsTask.Result);
            pullRequest.UpdateDiscussionComments(generalPrCommentsTask.Result);
            pullRequest.UpdateStateTransitionComments(approvalSummariesTask.Result);
            pullRequest.IsFullyInterrogated = true;

            return true;
        }
    }
}