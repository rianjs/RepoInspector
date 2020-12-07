using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RepoMan.Records
{
    public class PullRequest
    {
        public string Title { get; set; }
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
        /// <param name="prReviewComments"></param>
        /// <returns></returns>
        public void UpdateDiffComments(IEnumerable<Comment> prReviewComments)
        {
            DiffComments.AddRange(prReviewComments);
        }
        
        /// <summary>
        /// The comments associated with when someone clicks the Approve or Changes Requested button in the approval workflow 
        /// </summary>
        /// <returns></returns>
        public void UpdateStateTransitionComments(IEnumerable<Comment> commentsForStateTransition)
        {
            ReviewComments.AddRange(commentsForStateTransition);
        }
        
        /// <summary>
        /// The top-level comments on a pull request that are not associated with specific commits, lines of code, etc.
        /// </summary>
        /// <param name="generalPrComments"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public void UpdateDiscussionComments(IEnumerable<Comment> generalPrComments)
        {
            ReviewComments.AddRange(generalPrComments);
        }

        [JsonIgnore]
        public IEnumerable<Comment> AllComments
            => ReviewComments.Concat(DiffComments).Concat(CommitComments);
    }
}