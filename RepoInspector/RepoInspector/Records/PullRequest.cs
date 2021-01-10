using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RepoInspector.Records
{
    public class PullRequest
    {
        public string Title { get; set; }
        public int Number { get; set; }
        public long Id { get; set; }
        public string HtmlUrl { get; set; }
        public User Submitter { get; set; }
        public string Body { get; set; }
        public Comment BodyComment => new Comment
        {
            CreatedAt = OpenedAt,
            UpdatedAt = UpdatedAt,
            HtmlUrl = HtmlUrl,
            Id = Id,
            Text = Body,
            User = Submitter,
        };
        
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
        public List<Comment> Comments { get; set; } = new List<Comment>();
        
        /// <summary>
        /// The top-level comments on a pull request that are not associated with specific commits, lines of code, etc.
        /// </summary>
        /// <param name="generalPrComments"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public void AppendComments(IEnumerable<Comment> generalPrComments)
        {
            Comments.AddRange(generalPrComments);
        }

        /// <summary>
        /// Returns the Body of the Pull Request, along with all of the comments associated with it.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Comment> FullCommentary => Comments.Prepend(BodyComment);
    }
}