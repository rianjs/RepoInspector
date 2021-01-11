using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RepoInspector.Repository;

namespace RepoInspector.Records
{
    public class WatchedRepository :
        IEquatable<WatchedRepository>
    {
        /// <summary>
        /// Some git providers require a login along with a bearer token. Some don't.
        /// </summary>
        public string Login { get; set; }
        
        /// <summary>
        /// API token for making requests to the API
        /// </summary>
        public string ApiToken { get; set; }
        
        /// <summary>
        /// The type of git repository.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public RepositoryKind RepositoryKind { get; set; }

        /// <summary>
        /// The URL for the git instance. This might be "github.com" or "github.company.com" or "company.com/git"
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// The user or organization that owns the code
        /// </summary>
        public string Owner { get; set; }
        
        /// <summary>
        /// The repository name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The mainline branch. Usually "master", or "main", or "default"
        /// </summary>
        public string MainBranch { get; set; }
        
        /// <summary>
        /// Optional repository description.
        /// </summary>
        public string Description { get; set; }

        public bool Equals(WatchedRepository other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Login, other.Login, StringComparison.Ordinal)
                && string.Equals(ApiToken, other.ApiToken, StringComparison.Ordinal)
                && RepositoryKind == other.RepositoryKind
                && string.Equals(Url, other.Url, StringComparison.Ordinal)
                && string.Equals(Owner, other.Owner, StringComparison.Ordinal)
                && string.Equals(Name, other.Name, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((WatchedRepository) obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Login, StringComparer.Ordinal);
            hashCode.Add(ApiToken, StringComparer.Ordinal);
            hashCode.Add((int) RepositoryKind);
            hashCode.Add(Url, StringComparer.Ordinal);
            hashCode.Add(Owner, StringComparer.Ordinal);
            hashCode.Add(Name, StringComparer.Ordinal);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(WatchedRepository left, WatchedRepository right)
            => Equals(left, right);

        public static bool operator !=(WatchedRepository left, WatchedRepository right)
            => !Equals(left, right);
    }
}