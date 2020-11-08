using System;
using System.Collections.Generic;

namespace RepoMan
{
    public class RepositoryDetails
    {
        public long Id { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime PushedAt { get; set; }
        public long Size { get; set; }
        public bool IsArchived { get; set; }
    }
    public class PullRequestDetails
    {
        public long Id { get; set; }
        public string Url { get; set; }
        public long Number { get; set; }
        public List<Comment> Comments { get; set; }
        public List<Comment> ReviewComments { get; set; }
        public User Submitter { get; set; }
        
        /// <summary>
        /// Open, closed, merged, etc.
        /// </summary>
        public string State { get; set; }
        
        public DateTime OpenTimestamp { get; set; }
        public DateTime CloseTimestamp { get; set; }
        public DateTime MergeTimestamp { get; set; }
    }

    public class Comment
    {
        public long Id { get; set; }
        public User User { get; set; }
        public DateTime Timestamp { get; set; }
        public string Text { get; set; }
    }

    public class User
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
    }
}