using System;
using System.Collections.Generic;
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
            var repoHealthAnalyzer = new RepoHealthAnalyzer();
            var fs = new Filesystem();
            var cacheManager = new FilesystemCacheManager(fs, _scratchDir, _jsonSerializerSettings);
            var dosBuffer = TimeSpan.FromSeconds(0.1);
            
            var watchedRepos = GetWatchedRepositories()
                .GroupBy(r => r.ApiToken);

            var repoMgrInitializationQuery =
                from kvp in watchedRepos
                from repo in kvp
                let client = GetClient(repo.BaseUrl, kvp.Key)
                let prReader = new GitHubRepoPullRequestReader(repo.Owner, repo.RepositoryName, client)
                select RepositoryManager.InitializeAsync(
                    repo.Owner,
                    repo.RepositoryName,
                    prReader,
                    cacheManager,
                    dosBuffer,
                    refreshFromUpstream: true,
                    _logger);
            var watcherInitializationTasks = repoMgrInitializationQuery.ToList();
            await Task.WhenAll(watcherInitializationTasks);

            var repoWorkers = watcherInitializationTasks
                .Select(t => t.Result)
                .Select(rm => new RepoWorker(rm, approvalAnalyzer, commentAnalyzer, wordCounter, repoHealthAnalyzer, _logger))
                .ToList();
            
            // Create a BackgroundService with the collection of workers, and update the stats every 4 hours or so
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