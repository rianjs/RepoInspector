using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Octokit;

namespace RepoMan.Repository
{
    public class TargetRepository
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
    
    public class PullRequestDetails :
        IEquatable<PullRequestDetails>
    {
        public int Number { get; set; }
        public long Id { get; set; }
        public string HtmlUrl { get; set; }
        public User Submitter { get; set; }
        
        public string Body { get; set; }
        
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
            => state == PullRequestReviewState.Commented
                ? null
                : state.StringValue;

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

        [JsonIgnore]
        public IEnumerable<Comment> AllComments
            => ReviewComments.Concat(DiffComments).Concat(CommitComments);

        public bool Equals(PullRequestDetails other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Number == other.Number
                && UpdatedAt.Equals(other.UpdatedAt);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((PullRequestDetails) obj);
        }

        public override int GetHashCode()
            => HashCode.Combine(Number, UpdatedAt);

        public static bool operator ==(PullRequestDetails left, PullRequestDetails right)
            => Equals(left, right);

        public static bool operator !=(PullRequestDetails left, PullRequestDetails right)
            => !Equals(left, right);
    }

    public class Comment :
        IEquatable<Comment>
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

        public bool Equals(Comment other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id
                && UpdatedAt.Equals(other.UpdatedAt);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Comment) obj);
        }

        public override int GetHashCode()
            => HashCode.Combine(Id, UpdatedAt);

        public static bool operator ==(Comment left, Comment right)
            => Equals(left, right);

        public static bool operator !=(Comment left, Comment right)
            => !Equals(left, right);
    }

    public class User :
        IEquatable<User>
    {
        public long Id { get; set; }
        public string Login { get; set; }
        public string HtmlUrl { get; set; }

        public bool Equals(User other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((User) obj);
        }

        public override int GetHashCode()
            => Id.GetHashCode();

        public static bool operator ==(User left, User right)
            => Equals(left, right);

        public static bool operator !=(User left, User right)
            => !Equals(left, right);
    }
}