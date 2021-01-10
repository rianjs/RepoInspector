using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using RepoInspector.Records;

namespace RepoInspector.Analysis.Scoring
{
    [JsonConverter(typeof(Scorer))]
    public abstract class Scorer :
        IEquatable<Scorer>
    {
        public abstract string Attribute { get; }
        public abstract double ScoreMultiplier { get; }
        public abstract Score GetScore(PullRequest prDetails);

        public bool Equals(Scorer other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Attribute, other.Attribute, StringComparison.OrdinalIgnoreCase)
                && ScoreMultiplier.Equals(other.ScoreMultiplier);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Scorer) obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Attribute, StringComparer.OrdinalIgnoreCase);
            hashCode.Add(ScoreMultiplier);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(Scorer left, Scorer right)
            => Equals(left, right);

        public static bool operator !=(Scorer left, Scorer right)
            => !Equals(left, right);
    }
    
    public abstract class PullRequestScorer : Scorer
    {
        public abstract int Count(PullRequest prDetails);

        public override Score GetScore(PullRequest prDetails)
        {
            var count = Count(prDetails);
            var rawPoints = count * ScoreMultiplier;
            var points = Math.Round(rawPoints, 2, MidpointRounding.AwayFromZero);
            return new Score
            {
                Attribute = Attribute,
                Count = count,
                Points = points,
            };
        }
    }
    
    public abstract class CommentExtractorScorer : PullRequestScorer
    {
        /// <summary>
        /// Returns the matching elements from the specified string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public abstract IEnumerable<string> Extract(string s);
    }
}