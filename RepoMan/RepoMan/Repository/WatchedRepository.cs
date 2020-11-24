using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RepoMan.Repository
{
    public class WatchedRepository
    {
        /// <summary>
        /// The URL for the git instance. This might be "github.com" or "github.company.com" or "company.com/git"
        /// </summary>
        public string BaseUrl { get; set; }
        
        /// <summary>
        /// The user or organization that owns the code
        /// </summary>
        public string Owner { get; set; }
        
        /// <summary>
        /// The repository name
        /// </summary>
        public string RepositoryName { get; set; }
        
        /// <summary>
        /// Optional repository description
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// API token for making requests to the API
        /// </summary>
        public string ApiToken { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public RepositoryKind RepositoryKind { get; set; }
    }
}