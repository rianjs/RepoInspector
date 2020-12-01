using System;

namespace RepoMan.Records
{
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
}