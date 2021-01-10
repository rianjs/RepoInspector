using System;
using Octokit;
using RepoInspector.Records;
using User = RepoInspector.Records.User;

namespace RepoInspector.Repository
{
    public static class OctokitExtensions
    {
        public static Comment FromIssueComment(this IssueComment issueComment)
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
                    Id = issueComment.User.Id.ToString(),
                    Login = issueComment.User.Login,
                },
            };
        }
        
        public static Comment FromPullRequestReviewComment(this PullRequestReviewComment prComment)
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
                    Id = prComment.User.Id.ToString(),
                    Login = prComment.User.Login,
                },
            };
        }
        
        public static Comment FromPullRequestReviewSummary(this PullRequestReview prReview)
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
                    Id = prReview.User.Id.ToString(),
                    Login = prReview.User.Login,
                },
                ReviewState = prReview.State.Value.FromPullRequestReviewState(),
            };
        }

        public static PullRequestReviewState FromPullRequestReviewState(this Octokit.PullRequestReviewState reviewState)
        {
            switch (reviewState)
            {
                case Octokit.PullRequestReviewState.Approved:
                    return PullRequestReviewState.Approved;
                case Octokit.PullRequestReviewState.ChangesRequested:
                    return PullRequestReviewState.ChangesRequested;
                case Octokit.PullRequestReviewState.Commented:
                    return PullRequestReviewState.Commented;
                case Octokit.PullRequestReviewState.Dismissed:
                    return PullRequestReviewState.Dismissed;
                case Octokit.PullRequestReviewState.Pending:
                    return PullRequestReviewState.Pending;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reviewState), reviewState, null);
            }
        }

        public static PullRequestReviewState FromOctokitString(this string reviewState)
        {
            if (reviewState.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                return PullRequestReviewState.Approved;
            }
            
            if (reviewState.Equals("CHANGES_REQUESTED", StringComparison.OrdinalIgnoreCase))
            {
                return PullRequestReviewState.ChangesRequested;
            }

            if (reviewState.Equals("COMMENTED", StringComparison.OrdinalIgnoreCase))
            {
                return PullRequestReviewState.Commented;
            }

            if (reviewState.Equals("DISMISSED", StringComparison.OrdinalIgnoreCase))
            {
                return PullRequestReviewState.Dismissed;
            }

            if (reviewState.Equals("PENDING", StringComparison.OrdinalIgnoreCase))
            {
                return PullRequestReviewState.Pending;
            }
            
            throw new ArgumentOutOfRangeException($"{nameof(reviewState)} value '{reviewState}' is not a recognized value");
        }

        public static string ToOctokitString(this PullRequestReviewState reviewState)
        {
            switch (reviewState)
            {
                case PullRequestReviewState.Unspecified:
                    return null;
                case PullRequestReviewState.Approved:
                    return "APPROVED";
                case PullRequestReviewState.ChangesRequested:
                    return "CHANGES_REQUESTED";
                case PullRequestReviewState.Commented:
                    return "COMMENTED";
                case PullRequestReviewState.Dismissed:
                    return "DISMISSED";
                case PullRequestReviewState.Pending:
                    return "PENDING";
                default:
                    throw new ArgumentOutOfRangeException(nameof(reviewState), reviewState, null);
            }
        }
        
        public static ItemState FromItemStateFilter(this ItemStateFilter filter)
        {
            switch (filter)
            {
                case ItemStateFilter.Open:
                    return ItemState.Open;
                case ItemStateFilter.Closed:
                    return ItemState.Closed;
                case ItemStateFilter.All:
                    return ItemState.All;
                default:
                    throw new ArgumentOutOfRangeException(nameof(filter), filter, null);
            }
        }

        public static ItemStateFilter ToItemStateFilter(this ItemState itemState)
        {
            switch (itemState)
            {
                case ItemState.Open:
                    return ItemStateFilter.Open;
                case ItemState.Closed:
                    return ItemStateFilter.Closed;
                case ItemState.All:
                    return ItemStateFilter.All;
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemState), itemState, null);
            }
        }
    }
}