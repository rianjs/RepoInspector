using System;
using System.Text.Json.Serialization;
using RepoMan.Serialization;

namespace RepoMan.Analysis.Scoring
{
    public class Score :
        IEquatable<Score>
    {
        public string Attribute { get; set; }
        public int Count { get; set; }
        
        [JsonConverter(typeof(TruncatingDoubleConverter))]
        public double Points { get; set; }

        public bool Equals(Score other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Attribute == other.Attribute
                && Count == other.Count
                && Points.Equals(other.Points);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Score) obj);
        }

        public override int GetHashCode()
            => HashCode.Combine(Attribute, Count, Points);

        public static bool operator ==(Score left, Score right)
            => Equals(left, right);

        public static bool operator !=(Score left, Score right)
            => !Equals(left, right);
    }
}