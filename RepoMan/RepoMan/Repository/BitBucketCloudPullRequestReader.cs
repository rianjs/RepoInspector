using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RepoMan.Records;
using ILogger = Serilog.ILogger;

namespace RepoMan.Repository
{
    public class BitBucketCloudPullRequestReader :
        IRepoPullRequestReader
    {
        private readonly string _prApiUrl;
        private readonly string _repoTag;
        private readonly HttpClient _bbClient;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly IClock _clock;
        private readonly ILogger _logger;

        public BitBucketCloudPullRequestReader(string hostname, string repoOwner, string repoName, HttpClient client, JsonSerializerSettings jsonSerializerSettings, IClock clock, ILogger logger)
        {
            if (!Uri.TryCreate(hostname, UriKind.Absolute, out var _)) throw new ArgumentException($"'{hostname}' is not a valid URL");
            if (string.IsNullOrWhiteSpace(repoOwner)) throw new ArgumentNullException(nameof(repoOwner));
            if (string.IsNullOrWhiteSpace(repoName)) throw new ArgumentNullException(nameof(repoName));
            _repoTag = $"{repoOwner}:{repoName}";
            _jsonSerializerSettings = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bbClient = client ?? throw new ArgumentNullException(nameof(client));

            var rawHostname = string.IsNullOrWhiteSpace(hostname)
                ? throw new ArgumentNullException(nameof(hostname))
                : hostname;
            
            const string apiSlug = "!api/2.0";

            var normalizedApiHost = rawHostname.EndsWith("/", StringComparison.Ordinal)
                ? $"{hostname}{apiSlug}"
                : $"{hostname}/{apiSlug}";

            _prApiUrl = $"{normalizedApiHost}/repositories/{repoOwner}/{repoName}/pullrequests";
        }

        public async Task<IList<PullRequest>> GetPullRequestsRootAsync(ItemState stateFilter, DateTimeOffset lastCheck)
        {
            var prStateFilters = stateFilter.GetBbCloudStateFilter();
            var updateAtFilter = lastCheck.GetBbCloudUpdatedAtFilter(_clock);
            var completeArgs = BitBucketCloudExtensions.BuildFullQuery(new[]{prStateFilters, updateAtFilter}, " AND ");
            
            var encoded = WebUtility.UrlEncode(completeArgs);
            var url = $"{_prApiUrl}?q={encoded}";
            
            // Get the first page: https://bitbucket.org/!api/2.0/repositories/{repoOwner}/{repoName}/pullrequests
            // Follow `next` until it's null
            _logger.Information($"Pulling first page of {_repoTag}");
            var timer = Stopwatch.StartNew();
            var page = await GetBitbucketPullRequests(url);
            var aggregatePrs = new List<BitbucketPullRequest>(page.PullRequests);

            if (!string.IsNullOrWhiteSpace(page.next))
            {
                _logger.Information($"Approximately {page.size:N0} pull requests to fetch in pages of size {page.pagelen:N0}");
                var requestsToMake = page.size / (float) page.pagelen;
                var approxPages = Convert.ToInt32(Math.Ceiling(requestsToMake));
                var counter = 0;
                var next = page.next;
                while (!string.IsNullOrWhiteSpace(next))
                {
                    _logger.Information($"{_repoTag} - pulling page {++counter:N0} / ~{approxPages:N0}");
                    var pageTimer = Stopwatch.StartNew();
                    var nextPage = await GetBitbucketPullRequests(next);
                    pageTimer.Stop();
                    next = nextPage.next;
                    aggregatePrs.AddRange(nextPage.PullRequests);
                    _logger.Information($"{_repoTag} - page {counter:N0} / {approxPages:N0} pulled in ~{pageTimer.ElapsedMilliseconds:N0}ms");
                }
            }
            timer.Stop();
            _logger.Information($"{aggregatePrs.Count:N0} discovered in {timer.ElapsedMilliseconds:N0}ms");

            var pullRequests = aggregatePrs
                .Select(bbPr => bbPr.ToPullRequest())
                .ToList();
            return pullRequests;
        }

        private async Task<BbCloudPullRequestListResult> GetBitbucketPullRequests(string url)
        {
            using var response = await _bbClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var listResult = JsonConvert.DeserializeObject<BbCloudPullRequestListResult>(json);
            return listResult;
        }
        
        public Task<IList<PullRequest>> GetPullRequestsRootAsync(ItemState stateFilter)
            => GetPullRequestsRootAsync(stateFilter, DateTimeOffset.MaxValue);

        public Task<bool> TryFillCommentGraphAsync(PullRequest pullRequest)
        {
            // The simplest way to get the comments for a PR is to query for `activities` which will show a lot more than comments, but it a reasonably complete
            // representation of stuff that happened on a PR. The comments API is fairly incomplete.

            // Get the first page: https://bitbucket.org/!api/2.0/repositories/{repoOwner}/{repoName}/pullrequests/{prNumber}/activity
            // Follow next until it's null

            throw new System.NotImplementedException();
        }
        
        #region BitbucketDeserializationHelpers
        
        private class BbCloudPullRequestListResult
        {
            public int pagelen { get; set; }
            public string next { get; set; }
            public int page { get; set; }
            public int size { get; set; }
            [JsonProperty("values")]
            public List<BitbucketPullRequest> PullRequests { get; set; }
        }

        private class BitbucketPullRequest
        {
            [JsonProperty("id")]
            public int Number { get; set; }
            
            [JsonProperty("description")]
            public string Title { get; set; }
            
            [JsonProperty("author")]
            public BitBucketUser Submitter { get; set; }
            
            [JsonProperty("created_on")]
            public DateTimeOffset CreatedAt { get; set; }
            
            [JsonProperty("updated_on")]
            public DateTimeOffset UpdatedAt { get; set; }
            
            [JsonProperty("comment_count")]
            public int CommentCount { get; set; }
            
            public Summary Summary { get; set; }
            
            public string State { get; set; }
            public Dictionary<string, Link> Links { get; set; }

            public PullRequest ToPullRequest()
            {
                var pr = new PullRequest
                {
                    Title = Title.Trim(),
                    Number = Number,
                    Id = Number,
                    HtmlUrl = Links["html"].Href,
                    State = State,
                    Submitter = Submitter.ToUser(),
                    OpenedAt = CreatedAt,
                    UpdatedAt = UpdatedAt,
                    Body = Summary.Raw.Trim(),
                };
                return pr;
            }
        }

        private class BitBucketUser
        {
            [JsonProperty("uuid")]
            public string Id { get; set; }
            
            [JsonProperty("nickname")]
            public string Login { get; set; }
            
            public Dictionary<string, Link> Links { get; set; }

            public User ToUser()
            {
                var user = new User
                {
                    Login = Login,
                    Id = Id,
                    HtmlUrl = Links["html"].Href,
                };
                return user;
            }
        }
        
        public class Link
        {
            public string Href { get; set; }
        }
        
        public class Summary
        {
            public string Raw { get; set; }
        }
        
        #endregion
    }
}