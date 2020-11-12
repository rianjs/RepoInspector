using System;
using System.Collections.Generic;
using System.Linq;
using Octokit;

namespace RepoMan
{
    public class RepositoryDetails
    {
        public long Id { get; set; }
        public string Url { get; set; }
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
        /// The top-level comments associated with a pull request
        /// </summary>
        public List<Comment> Comments { get; set; }
        
        /// <summary>
        /// Comments associated with clicking the button in the approve/request changes workflow
        /// </summary>
        public List<Comment> ReviewComments { get; } = new List<Comment>();
        
        /// <summary>
        /// Comments on specific parts of the diff
        /// </summary>
        public List<Comment> DiffComments { get; } = new List<Comment>();
        
        /// <summary>
        /// Comments on specific commits
        /// </summary>
        public List<Comment> CommitComments { get; } = new List<Comment>();
        
        /// <summary>
        /// Open, closed, merged, etc.
        /// </summary>
        public string State { get; set; }
        
        public DateTimeOffset OpenTimestamp { get; set; }
        public DateTimeOffset CloseTimestamp { get; set; }
        public DateTimeOffset MergeTimestamp { get; set; }
        
        public void WithPullRequestReviewComments(IList<PullRequestReviewComment> prReviewComments)
        {
            var reviewCommentsQuery = prReviewComments.Select(c => new Comment
            {
                Id = c.Id,
                HtmlUrl = c.HtmlUrl,
                Text = c.Body,
                Timestamp = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                User = new User
                {
                    Id = c.User.Id,
                    Login = c.User.Login,
                },
            });
            
            ReviewComments.AddRange(reviewCommentsQuery);
        }
    }

    public class Comment
    {
        public long Id { get; set; }
        public User User { get; set; }
        public string HtmlUrl { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public string Text { get; set; }
    }

    public class User
    {
        public long Id { get; set; }
        public string Login { get; set; }
    }
}