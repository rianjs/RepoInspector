namespace RepoMan.Repository
{
    public class RepositoryDetails
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
        public string Repository { get; set; }
        
        /// <summary>
        /// API token for making requests to the API
        /// </summary>
        public string ApiToken { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public RepositoryKind RepositoryKind { get; set; }
    }
}