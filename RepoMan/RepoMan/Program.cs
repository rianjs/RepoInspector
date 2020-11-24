using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Octokit;
using RepoMan.Analysis;
using RepoMan.IO;
using RepoMan.PullRequest;
using RepoMan.Repository;
using Serilog;

namespace RepoMan
{
    class Program
    {
        private static readonly string _tokenPath = Path.Combine(GetScratchDirectory(), "repoman-pan.secret");
        private static readonly string _scratchDir = GetScratchDirectory();
        private static readonly string _url = "https://github.com";
        private static readonly string _token = File.ReadAllText(_tokenPath).Trim();
        private static readonly JsonSerializerSettings _jsonSerializerSettings = GetDebugJsonSerializerSettings();
        private static readonly ILogger _logger = GetLogger();

        static async Task Main(string[] args)
        {
            await Task.Delay(0);

            var explicitApprovals = GetExplicitApprovals();
            var implicitApprovals = GetImplicitApprovals();
            var explicitNonApprovals = GetExplicitNonApprovals();
            var wordCounter = new WordCounter();
            var approvalAnalyzer = new ApprovalAnalyzer(explicitApprovals, explicitNonApprovals, implicitApprovals);
            var commentAnalyzer = new CommentAnalyzer(approvalAnalyzer, wordCounter);
            var fs = new Filesystem();
            var cacheManager = new FilesystemCacheManager(fs, _scratchDir, _jsonSerializerSettings);
            var dosBuffer = TimeSpan.FromSeconds(0.5);
            
            // Read the list of repos to check
            var watchedRepos = GetWatchedRepositories()
                .GroupBy(r => r.ApiToken);

            var watcherInitializationQuery =
                from kvp in watchedRepos
                from repo in kvp
                let client = GetClient(repo.BaseUrl, kvp.Key)
                let prReader = new GitHubRepoPullRequestReader(repo.Owner, repo.RepositoryName, client)
                select RepositoryWatcher.InitializeAsync(
                    repo.Owner,
                    repo.RepositoryName,
                    prReader,
                    cacheManager,
                    dosBuffer,
                    refreshFromUpstream: true,
                    _logger);
            var watcherInitializationTasks = watcherInitializationQuery.ToList();
            await Task.WhenAll(watcherInitializationTasks);

            var initializedWatchers = watcherInitializationTasks
                .Select(t => t.Result)
                .ToList();
            
            // Create a BackgroundService with the collection of watchers, and update the stats every 4 hours or so
        }

        private static async Task Debug(GitHubClient client)
        {
            try
            {
                var pullRequestSnapshots = await repoHistoryMgr.GetPullRequestsAsync();
                
                _logger.Information($"Starting deep evaluation of {pullRequestSnapshots.Count} pull requests");
                var singularCommentComputeTimer = Stopwatch.StartNew();
                var singularPrSnapshots = pullRequestSnapshots
                    .Select(pr => commentAnalyzer.CalculateCommentStatistics(pr))
                    .ToDictionary(pr => pr.Number, pr => pr);
                singularCommentComputeTimer.Stop();
                _logger.Information($"Deep evaluation of {pullRequestSnapshots.Count} pull requests completed in {singularCommentComputeTimer.Elapsed.ToMicroseconds():N0} microseconds");

                _logger.Information($"Calculating repository health statistics for {owner}:{repo} repository which has {pullRequestSnapshots.Count:N0} pull requests");
                var repoHealthTimer = Stopwatch.StartNew();
                var repoHealth = repoHealthAnalyzer.CalculateRepositoryHealthStatistics(singularPrSnapshots.Values);
                repoHealthTimer.Stop();
                _logger.Information($"Repository health statistics for {owner}:{repo} calculated in {repoHealthTimer.Elapsed.ToMicroseconds():N0} microseconds");
                _logger.Information($"{Environment.NewLine}{JsonConvert.SerializeObject(repoHealth, _jsonSerializerSettings)}");
                
                // Other
                // Commits merged directly to master
                
                Console.WriteLine("Done");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Clients are intended to be reused for each top-level URL. So you can re-use a github.com pull request reader for every repo at github.com.  
        /// </summary>
        /// <param name="repoUrl"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static GitHubClient GetClient(string repoUrl, string token)
        {
            var github = new Uri(repoUrl);
            var client = new GitHubClient(new ProductHeaderValue("repoman-health-metrics"), github);
            var auth = new Credentials(token);
            client.Credentials = auth;
            return client;
        }

        private static string GetScratchDirectory()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            const int netCoreNestingLevel = 5;

            for (var i = 0; i < netCoreNestingLevel; i++)
            {
                path = Directory.GetParent(path).FullName;
            }

            return Path.Combine(path, "scratch");
        }
        
        private static ILogger GetLogger() =>
            new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        
        private static JsonSerializerSettings GetDebugJsonSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                //For demo purposes:
                DefaultValueHandling = DefaultValueHandling.Include,
                Formatting = Formatting.Indented,
                //Otherwise:
                // DefaultValueHandling = DefaultValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Converters = new List<JsonConverter> { new StringEnumConverter(), },
            };
        }

        private static List<string> GetImplicitApprovals()
        {
            return new List<string>
            {
                "lgtm",
                "ok to merge",
                "go ahead and merge",
                "looks good to me",
            };
        }

        private static List<string> GetExplicitNonApprovals()
        {
            return new List<string>
            {
                "CHANGES_REQUESTED",
                "COMMENTED",
                "DISMISSED",
                "PENDING",
            };
        }

        private static List<string> GetExplicitApprovals()
        {
            return new List<string>
            {
                "APPROVED"
            };
        }

        private static List<WatchedRepository> GetWatchedRepositories()
        {
            return new List<WatchedRepository>
            {
                new WatchedRepository
                {
                    Owner = "alex",
                    RepositoryName = "nyt-2020-election-scraper",
                    Description = "NYT election data scraper and renderer",
                    ApiToken = _token,
                    BaseUrl = "https://github.com",
                    RepositoryKind = RepositoryKind.GitHub,
                },
                new WatchedRepository
                {
                    Owner = "rianjs",
                    RepositoryName = "ical.net",
                    Description = "RFC-5545 ical data library",
                    ApiToken = _token,
                    BaseUrl = "https://github.com",
                    RepositoryKind = RepositoryKind.GitHub,
                },
            };
        }
    }
}