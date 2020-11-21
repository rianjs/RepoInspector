using System;
using System.Collections.Generic;
using System.Linq;
using Octokit;

namespace RepoMan.PullRequest
{
    public class RepositoryDetails
    {
        public long Id { get; set; }
        public string HtmlUrl { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public DateTimeOffset PushedAt { get; set; }
        public long Size { get; set; }
        public bool IsArchived { get; set; }
    }
    
    public class PullRequestDetails
    {
        public long Id { get; set; }
        public string HtmlUrl { get; set; }
        public int Number { get; set; }
        public User Submitter { get; set; }
        /// <summary>
        /// Open, closed, merged, etc.
        /// </summary>
        public string State { get; set; }
        public DateTimeOffset OpenedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        /// <summary>
        /// If the pull request hasn't been closed, this will have a value of DateTimeOffset.MaxValue
        /// </summary>
        public DateTimeOffset ClosedAt { get; set; }
        /// <summary>
        /// If the pull request hasn't been merged, this will have a value of DateTimeOffset.MaxValue
        /// </summary>
        public DateTimeOffset MergedAt { get; set; }
        
        /// <summary>
        /// Comments associated with clicking the button in the approve/request changes workflow
        /// </summary>
        public List<Comment> ReviewComments { get; set; } = new List<Comment>();
        
        /// <summary>
        /// Comments on specific parts of the diff
        /// </summary>
        public List<Comment> DiffComments { get; set; } = new List<Comment>();
        
        /// <summary>
        /// Comments on specific commits
        /// </summary>
        public List<Comment> CommitComments { get; set; } = new List<Comment>();
        
        public bool IsFullyInterrogated { get; set; }
        
        public PullRequestDetails(){}

        public PullRequestDetails(Octokit.PullRequest pullRequest)
        {
            if (pullRequest is null)
            {
                throw new ArgumentNullException(nameof(pullRequest));
            }
            
            Id = pullRequest.Id;
            Number = pullRequest.Number;
            HtmlUrl = pullRequest.HtmlUrl;
            Submitter = new User
            {
                Id = pullRequest.User.Id,
                Login = pullRequest.User.Login,
                HtmlUrl = pullRequest.User.HtmlUrl,
            };
            State = pullRequest.State.ToString();
            OpenedAt = pullRequest.CreatedAt;
            UpdatedAt = pullRequest.UpdatedAt;
            ClosedAt = pullRequest.ClosedAt ?? DateTimeOffset.MaxValue;
            MergedAt = pullRequest.MergedAt ?? DateTimeOffset.MaxValue;
        }
        
        /// <summary>
        /// The comments associated with a line of code, or range of lines of code.
        /// </summary>
        /// <param name="prDetails"></param>
        /// <param name="prReviewComments"></param>
        /// <returns></returns>
        public void UpdateDiffComments(IEnumerable<PullRequestReviewComment> prReviewComments)
        {
            DiffComments.AddRange(prReviewComments.Select(GetComment));
        }
        
        private static Comment GetComment(PullRequestReviewComment prComment)
        {
            return new Comment
            {
                Id = prComment.Id,
                HtmlUrl = prComment.HtmlUrl,
                Text = prComment.Body,
                CreatedAt = prComment.CreatedAt,
                UpdatedAt = prComment.UpdatedAt,
                User = new User
                {
                    Id = prComment.User.Id,
                    Login = prComment.User.Login,
                },
            };
        }
        
        /// <summary>
        /// The comments associated with when someone clicks the Approve or Changes Requested button in the approval workflow 
        /// </summary>
        /// <param name="prDetails"></param>
        /// <param name="stateTransitionComments"></param>
        /// <returns></returns>
        public void UpdateStateTransitionComments(IEnumerable<PullRequestReview> commentsForStateTransition)
        {
            ReviewComments.AddRange(commentsForStateTransition.Select(GetComment));
        }
        
        private static Comment GetComment(PullRequestReview prReview)
        {
            return new Comment
            {
                Id = prReview.Id,
                HtmlUrl = prReview.HtmlUrl,
                Text = prReview.Body,
                CreatedAt = prReview.SubmittedAt,
                UpdatedAt = DateTimeOffset.MinValue,
                User = new User
                {
                    Id = prReview.User.Id,
                    Login = prReview.User.Login,
                },
                ReviewState = GetReviewState(prReview.State),
            };
        }
        
        private static string GetReviewState(StringEnum<PullRequestReviewState> state)
        {
            if (state == PullRequestReviewState.Commented)
            {
                return null;
            }

            return state.StringValue;
        }
        
        /// <summary>
        /// The top-level comments on a pull request that are not associated with specific commits, lines of code, etc.
        /// </summary>
        /// <param name="generalPrComments"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public void UpdateDiscussionComments(IReadOnlyList<IssueComment> generalPrComments)
        {
            ReviewComments.AddRange(generalPrComments.Select(GetComment));
        }

        private static Comment GetComment(IssueComment issueComment)
        {
            return new Comment
            {
                Id = issueComment.Id,
                HtmlUrl = issueComment.HtmlUrl,
                Text = issueComment.Body,
                CreatedAt = issueComment.CreatedAt,
                UpdatedAt = issueComment.UpdatedAt ?? DateTimeOffset.MinValue,
                User = new User
                {
                    Id = issueComment.User.Id,
                    Login = issueComment.User.Login,
                },
            };
        }

        public IEnumerable<Comment> AllComments
            => ReviewComments.Concat(DiffComments).Concat(CommitComments);
    }

    public class Comment
    {
        public long Id { get; set; }
        public User User { get; set; }
        public string HtmlUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        
        /// <summary>
        /// Null, unless the comment is associated with a review comment where someone has approved it, or requested changes or whatever
        /// </summary>
        public string ReviewState { get; set; }
        public string Text { get; set; }
    }

    public class User
    {
        public long Id { get; set; }
        public string Login { get; set; }
        public string HtmlUrl { get; set; }
    }
}