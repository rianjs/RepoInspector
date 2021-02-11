using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RepoInspector.Records;

namespace RepoInspector.Repository
{
    /// <summary>
    /// Groups IRepositoryReaders by bearer token. 
    /// </summary>
    public class BitBucketCloudPullRequestReaderFactory
    {
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly IClock _clock;
        private readonly Dictionary<BitBucketCloudLogin, HttpClient> _clientsByApiKey;
        private readonly HttpMessageHandler _messageHandler;
        private readonly ILogger _logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="clock"></param>
        /// <param name="jsonSerializerSettings"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public BitBucketCloudPullRequestReaderFactory(IClock clock, JsonSerializerSettings jsonSerializerSettings, IOptionsSnapshot<RepoInspectorOptions> optionsSnapshot, ILogger logger)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _jsonSerializerSettings = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _messageHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = optionsSnapshot.Value.HttpConnectionLifetime,
                // TODO: Convert this to .All when we can upgrade to .NET 5+
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
            };
            _clientsByApiKey = new Dictionary<BitBucketCloudLogin, HttpClient>();
        }

        public IRepoPullRequestReader GetReader(WatchedRepository repo)
        {
            if (repo is null) throw new ArgumentNullException(nameof(repo));
            if (repo.RepositoryKind != RepositoryKind.BitBucketCloud)
            {
                throw new ArgumentException($"Watched repository must be of type {RepositoryKind.BitBucketCloud}, but was {repo.RepositoryKind}");
            }

            var httpClient = GetClient(repo.Login, repo.ApiToken, repo.Url);
            return new BitBucketCloudPullRequestReader(
                repo.Url,
                repo.Owner,
                repo.Name,
                httpClient,
                _jsonSerializerSettings,
                _clock,
                _logger);
        }
        
        /// <summary>
        /// BitBucket Cloud's notion of a bearer token is called an "App password", and works in cases where 2FA is enabled, but some scoped access is required. It is
        /// probably sufficient for our use cases. BBCloud does NOT require a bearer token when accessing public repositories via the API.
        /// </summary>
        /// <param name="username">Can be null if the repository is public. If specified, a token must also be specified.</param>
        /// <param name="apiToken">Can be null if the repository is public. If a token is specified, a username must also be specified</param>
        /// <param name="repoUrl"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">If the repository URL is null or empty</exception>
        private HttpClient GetClient(string username, string apiToken, string repoUrl)
        {
            if (string.IsNullOrWhiteSpace(repoUrl)) throw new ArgumentNullException(nameof(repoUrl));

            var login = new BitBucketCloudLogin(username, apiToken);
            if (_clientsByApiKey.TryGetValue(login, out var existingClient))
            {
                return existingClient;
            }
            
            var client = new HttpClient(_messageHandler)
            {
                DefaultRequestHeaders = {Authorization = new AuthenticationHeaderValue("Basic", login.BasicAuth),},
            };
            
            _clientsByApiKey[login] = client;
            return client;
        }

        private class BitBucketCloudLogin :
            IEquatable<BitBucketCloudLogin>
        {
            public string Login { get; }
            public string ApiToken { get; }
            public string BasicAuth => GetBasicAuth();
            
            public BitBucketCloudLogin(string login, string apiToken)
            {
                if (!string.IsNullOrWhiteSpace(apiToken) && string.IsNullOrWhiteSpace(login))
                {
                    // API token without a login is invalid
                    throw new ArgumentException($"API tokens require a BitBucket Cloud username");
                }
                
                if (string.IsNullOrWhiteSpace(apiToken) && string.IsNullOrWhiteSpace(login))
                {
                    // No auth at all is OK
                    login = string.Empty;
                    apiToken = string.Empty;
                }
                
                Login = login;
                ApiToken = apiToken;
            }

            public bool Equals(BitBucketCloudLogin other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Login == other.Login && ApiToken == other.ApiToken;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj.GetType() == GetType() && Equals((BitBucketCloudLogin) obj);
            }

            public override int GetHashCode()
                => HashCode.Combine(Login, ApiToken);

            public string GetBasicAuth()
                => string.IsNullOrEmpty(ApiToken)
                    ? null
                    : Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Login}:{ApiToken}"));
        }
    }
}
