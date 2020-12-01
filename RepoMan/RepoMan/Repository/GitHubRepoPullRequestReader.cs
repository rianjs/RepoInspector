using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using RepoMan.Analysis.Normalization;
using RepoMan.Records;
using PullRequest = RepoMan.Records.PullRequest;
using User = RepoMan.Records.User;
using OctokitPullRequest = Octokit.PullRequest;

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
        private readonly INormalizer _bodyNormalizer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="repoOwner"></param>
        /// <param name="repoName"></param>
        /// <param name="client">This client instance may be used across GitHubRepoPullRequestReader instances</param>
        public GitHubRepoPullRequestReader(string repoOwner, string repoName, GitHubClient client, INormalizer bodyNormalizer)
        {
            _repoOwner = string.IsNullOrWhiteSpace(repoOwner)
                ? throw new ArgumentNullException(nameof(repoOwner))
                : repoOwner;
            
            _repoName = string.IsNullOrWhiteSpace(repoName)
                ? throw new ArgumentNullException(nameof(repoName))
                : repoName;

            _client = client ?? throw new ArgumentNullException(nameof(client));
            _bodyNormalizer = bodyNormalizer ?? throw new ArgumentNullException(nameof(bodyNormalizer));
        }

        /// <summary>
        /// Returns all of the closed Pull Requests associated with the repository. Makes no distinction between merged and unmerged.
        /// </summary>
        /// <param name="stateFilter"></param>
        /// <returns></returns>
        public async Task<IList<PullRequest>> GetPullRequestsRootAsync(ItemState stateFilter)
        {
            var prOpts = new PullRequestRequest
            {
                State = stateFilter.ToItemStateFilter(),
                SortProperty = PullRequestSort.Created,
                SortDirection = SortDirection.Ascending,
            };
            var pullRequests = await _client.PullRequest.GetAllForRepository(_repoOwner, _repoName, prOpts);
            
            var reduced = pullRequests
                .Select(FromPullRequest)
                .ToList();

            return reduced;
        }
        
        /// <summary>
        /// Fills out the comments on the pull request by doing concurrent calls to the various GitHub comment APIs, and aggregating the results
        /// </summary>
        /// <param name="pullRequest"></param>
        /// <returns></returns>
        public async Task<bool> TryFillCommentGraphAsync(PullRequest pullRequest)
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

            var diffReviewComments = diffReviewCommentsTask.Result.Select(c => c.FromPullRequestReviewComment());
            pullRequest.UpdateDiffComments(diffReviewComments);

            var generalPrComments = generalPrCommentsTask.Result.Select(c => c.FromIssueComment());
            pullRequest.UpdateDiscussionComments(generalPrComments);
            
            var stateTransitionComments = approvalSummariesTask.Result.Select(c => c.FromPullRequestReviewSummary());
            pullRequest.UpdateStateTransitionComments(stateTransitionComments);

            return true;
        }

        private PullRequest FromPullRequest(OctokitPullRequest octokitPr)
        {
            if (octokitPr is null)
            {
                throw new ArgumentNullException(nameof(octokitPr));
            }
            
            return new PullRequest
            {
                Number = octokitPr.Number,
                Id = octokitPr.Id,
                HtmlUrl = octokitPr.HtmlUrl,
                Submitter = new User
                {
                    Id = octokitPr.User.Id,
                    Login = octokitPr.User.Login,
                    HtmlUrl = octokitPr.User.HtmlUrl,
                },
                Body = _bodyNormalizer.Normalize(octokitPr.Body)?.Trim(),
                State = octokitPr.State.ToString(),
                OpenedAt = octokitPr.CreatedAt,
                UpdatedAt = octokitPr.UpdatedAt,
                ClosedAt = octokitPr.ClosedAt ?? DateTimeOffset.MaxValue,
                MergedAt = octokitPr.MergedAt ?? DateTimeOffset.MaxValue,
            };
        }

    }
}