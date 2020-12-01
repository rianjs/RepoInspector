using System;

namespace RepoMan.Records
{
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