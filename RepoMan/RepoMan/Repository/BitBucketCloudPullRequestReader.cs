using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RepoMan.Records;
using Microsoft.Extensions.Logging;

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

        public BitBucketCloudPullRequestReader(string hostname, string repoOwner, string repoName, HttpClient client,
            JsonSerializerSettings jsonSerializerSettings, IClock clock, ILogger logger)
        {
            if (!Uri.TryCreate(hostname, UriKind.Absolute, out var uriHost)) throw new ArgumentException($"'{hostname}' is not a valid URL");
            if (string.IsNullOrWhiteSpace(repoOwner)) throw new ArgumentNullException(nameof(repoOwner));
            if (string.IsNullOrWhiteSpace(repoName)) throw new ArgumentNullException(nameof(repoName));
            _repoTag = $"{repoOwner}:{repoName}";
            _jsonSerializerSettings = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bbClient = client ?? throw new ArgumentNullException(nameof(client));

            var authority = new Uri(uriHost.GetLeftPart(UriPartial.Authority), UriKind.Absolute);
            var fullUrl = new Uri(authority, "!api/2.0");
            _prApiUrl = $"{fullUrl}/repositories/{repoOwner}/{repoName}/pullrequests";
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
            _logger.LogInformation($"Pulling first page of {_repoTag}");
            var timer = Stopwatch.StartNew();
            var page = await GetBitbucketPullRequests(url);
            var aggregatePrs = new List<BitbucketPullRequest>(page.PullRequests);

            if (!string.IsNullOrWhiteSpace(page.next))
            {
                _logger.LogInformation($"Approximately {page.size:N0} pull requests to fetch in pages of size {page.pagelen:N0}");
                var requestsToMake = page.size / (float) page.pagelen;
                var approxPages = Convert.ToInt32(Math.Ceiling(requestsToMake));
                var counter = 0;
                var next = page.next;
                while (!string.IsNullOrWhiteSpace(next))
                {
                    _logger.LogInformation($"{_repoTag} - pulling page {++counter:N0} / ~{approxPages:N0}");
                    var pageTimer = Stopwatch.StartNew();
                    var nextPage = await GetBitbucketPullRequests(next);
                    pageTimer.Stop();
                    next = nextPage.next;
                    aggregatePrs.AddRange(nextPage.PullRequests);
                    _logger.LogInformation($"{_repoTag} - page {counter:N0} / ~{approxPages:N0} pulled in {pageTimer.ElapsedMilliseconds:N0}ms");
                }
            }
            timer.Stop();
            _logger.LogInformation($"{aggregatePrs.Count:N0} discovered in {timer.ElapsedMilliseconds:N0}ms");

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
            var listResult = JsonConvert.DeserializeObject<BbCloudPullRequestListResult>(json, _jsonSerializerSettings);
            return listResult;
        }
        
        public Task<IList<PullRequest>> GetPullRequestsRootAsync(ItemState stateFilter)
            => GetPullRequestsRootAsync(stateFilter, DateTimeOffset.MaxValue);

        public async Task<bool> TryFillCommentGraphAsync(PullRequest pullRequest)
        {
            // The simplest way to get the comments for a PR is to query for `activities` which will show a lot more than comments, but is a reasonably complete
            // representation of stuff that happened on a PR. The comments API is fairly incomplete.

            // Get the first page: https://bitbucket.org/!api/2.0/repositories/{repoOwner}/{repoName}/pullrequests/{prNumber}/activity
            // Follow next until it's null
            var url = $"{_prApiUrl}/{pullRequest.Number}/activity";
            var activities = new List<Activity>();
            var timer = Stopwatch.StartNew();
            PullRequestActivityList page = null;
            try
            {
                page = await GetPullRequestActivityListAsync(url);
                activities.AddRange(page.Activities);
            }
            catch (Exception e)
            {
                _logger.LogError($"Unable to get activities list: ' {url} '", e);
                return false;
            }

            if (string.IsNullOrWhiteSpace(page.next))
            {
                return true;
            }
            
            var completed = true;
            _logger.LogInformation($"Approximately {page.size:N0} pull requests to fetch in pages of size {page.pagelen:N0}");
            var requestsToMake = page.size / (float) page.pagelen;
            var approxPages = Convert.ToInt32(Math.Ceiling(requestsToMake));
            var counter = 0;
            var next = page.next;
            while (!string.IsNullOrWhiteSpace(next))
            {
                _logger.LogInformation($"{_repoTag} - pulling page {++counter:N0} / ~{approxPages:N0}");
                var pageTimer = Stopwatch.StartNew();
                PullRequestActivityList nextPage = null;
                try
                {
                    nextPage = await GetPullRequestActivityListAsync(next);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Unable to get activities list: ' {url} '", e);
                    _logger.LogInformation($"{_repoTag} - stopping pull request query activity for parent ' {url} '");
                    completed = false;
                    break;
                }

                pageTimer.Stop();
                next = nextPage.next;
                activities.AddRange(nextPage.Activities);
                _logger.LogInformation($"{_repoTag} - page {counter:N0} / ~{approxPages:N0} pulled in {pageTimer.ElapsedMilliseconds:N0}ms");
            }

            timer.Stop();
            _logger.LogInformation($"{activities.Count:N0} discovered in {timer.ElapsedMilliseconds:N0}ms");

            var reviewComments = activities
                .Select(ToComment)
                .Where(c => c is object);
            pullRequest.AppendComments(reviewComments);
            return completed;
        }

        private async Task<PullRequestActivityList> GetPullRequestActivityListAsync(string url)
        {
            using var response = await _bbClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var listResult = JsonConvert.DeserializeObject<PullRequestActivityList>(json, _jsonSerializerSettings);
            return listResult;
        }

        private Comment ToComment(Activity activity)
        {
            if (activity.Approval is object)
            {
                return new Comment
                {
                    Id = -1,
                    CreatedAt = activity.Approval.Date,
                    UpdatedAt = activity.Approval.Date,
                    User = activity.Approval.BbUser.ToUser(),
                    ReviewState = PullRequestReviewState.Approved,
                };
            }

            if (activity.Comment is object)
            {
                return new Comment
                {
                    Id = activity.Comment.Id,
                    Text = activity.Comment.Content.Raw.Trim(),
                    CreatedAt = activity.Comment.CreatedOn,
                    UpdatedAt = activity.Comment.UpdatedOn,
                    HtmlUrl = activity.Comment.Links["html"].Href,
                    ReviewState = null,
                    User = activity.Comment.User.ToUser(),
                };
            }

            return null;
        }

        #region BitbucketDeserializationHelpers
        
        private class PullRequestActivityList
        {
            public int pagelen { get; set; }
            public string next { get; set; }
            public int page { get; set; }
            public int size { get; set; }
            
            [JsonProperty("values")]
            public List<Activity> Activities { get; set; }
        }

        private class Activity
        {
            [JsonProperty("approval", NullValueHandling = NullValueHandling.Ignore)]
            public Approval Approval { get; set; }

            [JsonProperty("comment", NullValueHandling = NullValueHandling.Ignore)]
            public BitBucketCloudComment Comment { get; set; }
        }

        private class Approval
        {
            [JsonProperty("date")]
            public DateTimeOffset Date { get; set; }

            [JsonProperty("user")]
            public BitBucketUser BbUser { get; set; }
        }

        private class BitBucketCloudComment
        {
            public Dictionary<string, Link> Links { get; set; }

            [JsonProperty("deleted")]
            public bool Deleted { get; set; }

            [JsonProperty("content")]
            public BitBucketCloudCommentContent Content { get; set; }

            [JsonProperty("created_on")]
            public DateTimeOffset CreatedOn { get; set; }

            [JsonProperty("user")]
            public BitBucketUser User { get; set; }

            [JsonProperty("updated_on")]
            public DateTimeOffset UpdatedOn { get; set; }

            [JsonProperty("id")]
            public long Id { get; set; }
        }

        private class BitBucketCloudCommentContent
        {
            public string Raw { get; set; }

            // TODO: Figure out which is more appropriate: Raw or HTML. Unlike GitHub, Bitbucket stores HTML in its comments, and I think using raw will
            // TODO: cause any embedded URLs and/or code blocks to be lost, and therefore not scored
            public string Html { get; set; }
        }
        
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