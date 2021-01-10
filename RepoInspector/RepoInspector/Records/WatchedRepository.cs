using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RepoInspector.Repository;

namespace RepoInspector.Records
{
    public class WatchedRepository
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
    }
}