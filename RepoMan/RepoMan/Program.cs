using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Octokit;
using RepoMan.Analysis;
using RepoMan.Analysis.ApprovalAnalyzers;
using RepoMan.Analysis.Scoring;
using RepoMan.IO;
using RepoMan.Repository;
using Serilog;

namespace RepoMan
{
    class Program
    {
        private static readonly string _tokenPath = Path.Combine(GetScratchDirectory(), "repoman-pan.secret");
        private static readonly string _configPath = Path.Combine(GetScratchDirectory(), "repoman-config.json");
        private static readonly string _scratchDir = GetScratchDirectory();
        private static readonly string _url = "https://github.com";
        private static readonly string _token = File.ReadAllText(_tokenPath).Trim();
        private static readonly JsonSerializerSettings _jsonSerializerSettings = GetDebugJsonSerializerSettings();
        private static readonly ILogger _logger = GetLogger();

        static async Task Main(string[] args)
        {
            Console.WriteLine(Environment.CurrentDirectory);
            
            // There's probably an idiomatic way to tuck these into the DI container as referenceable values...
            var dosBuffer = TimeSpan.FromSeconds(0.1);
            // Also include: cache path, scratch dir, etc. All the static readonlys above

            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile(_configPath, optional: false, reloadOnChange: true)
                .Build();

            var serviceCollection = new ServiceCollection()
                .Configure<PullRequestConstants>(RepositoryKind.GitHub.ToString(),
                    configuration.GetSection("PRConstants:GitHub"))
                .Configure<PullRequestConstants>(RepositoryKind.BitBucket.ToString(),
                    configuration.GetSection("PRConstants:BitBucket"))
                .AddTransient(sp =>
                {
                    var prConstantsAccessor = sp.GetRequiredService<IOptionsSnapshot<PullRequestConstants>>();
                    var prConstants = prConstantsAccessor.Get(RepositoryKind.GitHub.ToString());
                    return new GitHubApprovalAnalyzer(
                        prConstants.ExplicitApprovals,
                        prConstants.ExplicitNonApprovals,
                        prConstants.ImplicitApprovals);
                }).AddTransient(sp =>
                {
                    var prConstantsAccessor = sp.GetRequiredService<IOptionsSnapshot<PullRequestConstants>>();
                    var prConstants = prConstantsAccessor.Get(RepositoryKind.BitBucket.ToString());
                    return new BitBucketApprovalAnalyzer(
                        prConstants.ExplicitApprovals,
                        prConstants.ExplicitNonApprovals,
                        prConstants.ImplicitApprovals);
                })
                // Reusable building blocks
                .AddSingleton<IFilesystem>(sp => new Filesystem())
                .AddSingleton<ICacheManager>(sp => new FilesystemCacheManager(sp.GetRequiredService<IFilesystem>(), _scratchDir, _jsonSerializerSettings))
                // CommentScorers
                .AddSingleton<CommentExtractorScorer, UrlScorer>()
                .AddSingleton<CommentExtractorScorer, CodeFenceScorer>()
                .AddSingleton<CommentExtractorScorer, CodeFragmentScorer>()
                .AddSingleton<CommentExtractorScorer, GitHubIssueLinkScorer>()
                // PullRequestScorers
                .AddSingleton<PullRequestScorer>(sp => new ApprovalScorer(sp.GetRequiredService<IApprovalAnalyzer>()))
                .AddSingleton<PullRequestScorer>(sp => new CommentCountScorer(sp.GetRequiredService<WordCountScorer>()))
                .AddSingleton<PullRequestScorer, WordCountScorer>()
                .AddSingleton<PullRequestScorer, BusinessDaysScorer>()
                .AddSingleton<PullRequestScorer, UniqueCommenterScorer>()
                // Roll it all up into the orchestrators...
                .AddSingleton(sp => new PullRequestAnalyzer(sp.GetRequiredService<IApprovalAnalyzer>(), sp.GetServices<PullRequestScorer>()))
                .AddSingleton(sp => new RepositoryAnalyzer());

            var serviceProvider = serviceCollection.BuildServiceProvider();
            
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
                    serviceProvider.GetRequiredService<ICacheManager>(),
                    dosBuffer,
                    refreshFromUpstream: true,
                    _logger);
            var watcherInitializationTasks = repoMgrInitializationQuery.ToList();
            await Task.WhenAll(watcherInitializationTasks);
            
            var repoWorkers = watcherInitializationTasks
                .Select(t => t.Result)
                .Select(repoManager => new RepoWorker(
                    repoManager,
                    serviceProvider.GetRequiredService<IPullRequestAnalyzer>(),
                    serviceProvider.GetRequiredService<IRepositoryAnalyzer>(),
                    _logger))
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
