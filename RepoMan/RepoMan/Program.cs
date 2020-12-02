using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
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
using RepoMan.Analysis.Normalization;
using RepoMan.Analysis.Scoring;
using RepoMan.IO;
using RepoMan.Records;
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
        private static readonly ILogger _logger = GetLogger();
        private static readonly CancellationTokenSource _cts = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            Console.WriteLine(Environment.CurrentDirectory);
            
            // There's probably an idiomatic way to tuck these into the DI container as referenceable values...
            var dosBuffer = TimeSpan.FromSeconds(0.1);
            var loopDelay = TimeSpan.FromHours(24);
            // Also include: cache path, scratch dir, etc. All the static readonlys above except the CTS, probably

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
                .AddSingleton<IClock, Clock>()
                .AddSingleton(sp => _logger)
                .AddSingleton<IWordCounter, WordCounter>()
                .AddSingleton<HtmlCommentStripper>()
                .AddSingleton(sp => GetKnownScorers(
                    approvalAnalyzer: sp.GetRequiredService<GitHubApprovalAnalyzer>(),
                    wc: sp.GetRequiredService<IWordCounter>()))
                .AddSingleton(sp => new ScorerConverter(sp.GetRequiredService<IScorerFactory>()))
                .AddSingleton(sp => GetDebugJsonSerializerSettings(sp.GetRequiredService<IScorerFactory>()))
                .AddSingleton(sp => new FilesystemDataProvider(sp.GetRequiredService<IFilesystem>(), _scratchDir, sp.GetRequiredService<JsonSerializerSettings>()))
                .AddSingleton<IPullRequestCacheManager>(sp => sp.GetRequiredService<FilesystemDataProvider>())
                .AddSingleton<IAnalysisManager>(sp => sp.GetRequiredService<FilesystemDataProvider>())
                // CommentScorers
                .AddSingleton<UrlScorer>()
                .AddSingleton<CodeFenceScorer>()
                .AddSingleton<CodeFragmentScorer>()
                .AddSingleton<GitHubIssueLinkScorer>()
                .AddSingleton<Scorer>(sp => sp.GetRequiredService<UrlScorer>())
                .AddSingleton<Scorer>(sp => sp.GetRequiredService<CodeFenceScorer>())
                .AddSingleton<Scorer>(sp => sp.GetRequiredService<CodeFragmentScorer>())
                .AddSingleton<Scorer>(sp => sp.GetRequiredService<GitHubIssueLinkScorer>())
                // PullRequestScorers
                .AddSingleton<WordCountScorer>()
                .AddSingleton<BusinessDaysScorer>()
                .AddSingleton<UniqueCommenterScorer>()
                .AddSingleton(sp => new CommentCountScorer(sp.GetRequiredService<IWordCounter>()))
                .AddSingleton(sp => new ApprovalScorer(sp.GetRequiredService<GitHubApprovalAnalyzer>()))
                .AddSingleton(sp => new CommentCountScorer(sp.GetRequiredService<IWordCounter>()))
                .AddSingleton<Scorer>(sp => sp.GetRequiredService<ApprovalScorer>())
                .AddSingleton<Scorer>(sp => sp.GetRequiredService<BusinessDaysScorer>())
                .AddSingleton<Scorer>(sp => sp.GetRequiredService<CommentCountScorer>())
                .AddSingleton<Scorer>(sp => sp.GetRequiredService<UniqueCommenterScorer>())
                .AddSingleton<Scorer>(sp => sp.GetRequiredService<WordCountScorer>())
                // Roll it all up into the orchestrators...
                .AddSingleton<IPullRequestAnalyzer>(sp => new PullRequestAnalyzer(sp.GetServices<Scorer>()))
                .AddSingleton<IRepositoryAnalyzer>(sp => new RepositoryAnalyzer(sp.GetRequiredService<IClock>()));

            var serviceProvider = serviceCollection.BuildServiceProvider();
            
            var watchedRepos = GetWatchedRepositories()
                .GroupBy(r => r.ApiToken);
            
            // TODO: Implement an UpgradeAsync method that can do one-time data transformations/updates on cached data

            var repoMgrInitializationQuery =
                from kvp in watchedRepos
                from repo in kvp
                let client = GetClient(repo.BaseUrl, kvp.Key)
                let prReader = new GitHubRepoPullRequestReader(
                    repo.Owner,
                    repo.RepositoryName,
                    client,
                    serviceProvider.GetRequiredService<HtmlCommentStripper>())
                select RepositoryManager.InitializeAsync(
                    repo.Owner,
                    repo.RepositoryName,
                    repo.BaseUrl,
                    prReader,
                    serviceProvider.GetRequiredService<IPullRequestCacheManager>(),
                    dosBuffer,
                    refreshFromUpstream: true,
                    _logger);
            var watcherInitializationTasks = repoMgrInitializationQuery.ToList();
            await Task.WhenAll(watcherInitializationTasks);
            
            var repoWorkerInitialization = watcherInitializationTasks
                .Select(t => t.Result)
                .Select(async repoManager => await RepoWorker.InitializeAsync(
                    repoManager,
                    serviceProvider.GetRequiredService<IPullRequestAnalyzer>(),
                    serviceProvider.GetRequiredService<IRepositoryAnalyzer>(),
                    serviceProvider.GetRequiredService<IAnalysisManager>(),
                    serviceProvider.GetRequiredService<IClock>(),
                    _logger))
                .ToList();
            await Task.WhenAll(repoWorkerInitialization);
            
            var loopServices = repoWorkerInitialization
                .Select(rwi => rwi.Result)
                .Select(rw => new LoopService(rw, loopDelay, _cts, _logger))
                .Select(l => l.LoopAsync())
                .ToList();
            await Task.WhenAll(loopServices);
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
        
        private static ILogger GetLogger()
        {
            return new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        }

        private static JsonSerializerSettings GetDebugJsonSerializerSettings(IScorerFactory scorerFactory)
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
                Converters = new List<JsonConverter> { new StringEnumConverter(), new TruncatingDoubleConverter(), new ScorerConverter(scorerFactory) },
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
        
        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            HandleShutdown();
        }

        private static void HandleShutdown(AssemblyLoadContext ctx)
            => HandleShutdown();

        private static void HandleShutdown()
        {
            _logger.Information("SIGTERM received. Shutting down");
            
            var stopwatch = Stopwatch.StartNew();
            _cts.Cancel();
            stopwatch.Stop();

            _logger.Information($"Daemon stopped in {stopwatch.ElapsedMilliseconds:N0}ms");
        }
        
        private static IScorerFactory GetKnownScorers(IApprovalAnalyzer approvalAnalyzer, IWordCounter wc)
        {
            var scorers = GetDerivedTypes<Scorer>(Assembly.GetAssembly(typeof(Scorer)));
            var scorerInstances = scorers
                .Select(s => CreateInstance(s, approvalAnalyzer, wc))
                .Cast<Scorer>()
                .ToDictionary(s => s.Attribute, StringComparer.OrdinalIgnoreCase);
            
            return new ScorerFactory(scorerInstances);
        }
        
        private static IEnumerable<Type> GetDerivedTypes<T>(Assembly assembly)
        {
            var derivedType = typeof(T);
            return assembly
                .GetTypes()
                .Where(t => t != derivedType && derivedType.IsAssignableFrom(t) && !t.IsAbstract);
        }
        
        private static object CreateInstance(Type s, IApprovalAnalyzer approvalAnalyzer, IWordCounter wc)
        {
            if (s == typeof(ApprovalScorer))
            {
                return Activator.CreateInstance(typeof(ApprovalScorer), approvalAnalyzer);
            }

            if (s == typeof(CommentCountScorer))
            {
                return Activator.CreateInstance(typeof(CommentCountScorer), wc);
            }

            if (s == typeof(WordCountScorer))
            {
                return Activator.CreateInstance(typeof(WordCountScorer), wc);
            }
            
            return Activator.CreateInstance(s);
        }
    }
}
