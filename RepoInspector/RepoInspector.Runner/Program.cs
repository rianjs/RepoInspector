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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using RepoInspector.Analysis;
using RepoInspector.Analysis.ApprovalAnalyzers;
using RepoInspector.Analysis.Normalization;
using RepoInspector.Analysis.Scoring;
using RepoInspector.IO;
using RepoInspector.Records;
using RepoInspector.Repository;

namespace RepoInspector.Runner
{
    class Program
    {
        private static readonly string _configPath = Path.Combine(GetScratchDirectory(), "repoman-config.json");
        private static readonly string _scratchDir = GetScratchDirectory();
        private static readonly ILogger _logger = GetLogger<Program>();
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
                .Configure<PullRequestConstants>(RepositoryKind.BitBucketCloud.ToString(),
                    configuration.GetSection("PRConstants:BitBucketCloud"))
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
                    var prConstants = prConstantsAccessor.Get(RepositoryKind.BitBucketServer.ToString());
                    return new BitBucketApprovalAnalyzer(
                        prConstants.ExplicitApprovals,
                        prConstants.ExplicitNonApprovals,
                        prConstants.ImplicitApprovals);
                })
                // Reusable building blocks
                .AddSingleton<IFilesystem>(sp => new Filesystem())
                .AddSingleton<IClock, Clock>()
                .AddSingleton<IWordCounter, WordCounter>()
                .AddSingleton<INormalizer, HtmlCommentStripper>()
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
                // Roll it all up into the orchestrators and factories...
                .AddSingleton<IPullRequestAnalyzer>(sp => new PullRequestAnalyzer(sp.GetServices<Scorer>()))
                .AddSingleton<IRepositoryAnalyzer>(sp => new RepositoryAnalyzer(sp.GetRequiredService<IClock>()))
                .AddSingleton(sp => new GitHubPullRequestReaderFactory("repoman-health-metrics", sp.GetRequiredService<INormalizer>()))
                .AddSingleton(sp => new BitBucketCloudPullRequestReaderFactory(
                    sp.GetRequiredService<IClock>(),
                    sp.GetRequiredService<JsonSerializerSettings>(),
                    httpConnectionLifespan: TimeSpan.FromSeconds(120),
                    GetLogger<BitBucketCloudPullRequestReaderFactory>()))
                .AddSingleton<IPullRequestReaderFactory, PullRequestReaderFactory>(sp => new PullRequestReaderFactory(
                    sp.GetRequiredService<GitHubPullRequestReaderFactory>(),
                    sp.GetRequiredService<BitBucketCloudPullRequestReaderFactory>(),
                    GetLogger<PullRequestReaderFactory>()))
                .AddSingleton<IRepoManagerFactory>(sp => new RepoManagerFactory(
                    sp.GetRequiredService<IPullRequestReaderFactory>(),
                    sp.GetRequiredService<IPullRequestCacheManager>(),
                    dosBuffer,
                    GetLogger<RepoManagerFactory>()));
            
            var serviceProvider = serviceCollection.BuildServiceProvider();
            
            // FUTURE: Consider implementing an UpgradeAsync method that can do one-time data transformations/updates on cached data

            var repos = configuration.GetSection("WatchedRepositories").Get<List<WatchedRepository>>();
            var repoManagers = repos
                .Select(r => serviceProvider.GetRequiredService<IRepoManagerFactory>().GetManagerAsync(r, refreshFromUpstream: false))
                .ToList();
            await Task.WhenAll(repoManagers);

            var repoWorkerInitialization = repoManagers
                .Select(t => t.Result)
                .Select(async repoManager => await RepoWorker.InitializeAsync(
                    repoManager,
                    serviceProvider.GetRequiredService<IPullRequestAnalyzer>(),
                    serviceProvider.GetRequiredService<IRepositoryAnalyzer>(),
                    serviceProvider.GetRequiredService<IAnalysisManager>(),
                    serviceProvider.GetRequiredService<IClock>(),
                    GetLogger<RepoWorker>()))
                .ToList();
            await Task.WhenAll(repoWorkerInitialization);
            
            var loopServices = repoWorkerInitialization
                .Select(rwi => rwi.Result)
                .Select(rw => new LoopService(rw, loopDelay, _cts, _logger))
                .Select(l => l.LoopAsync())
                .ToList();
            await Task.WhenAll(loopServices);
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
        
        private static ILogger<T> GetLogger<T>()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                    .AddConsole();
            });

            var logger = loggerFactory.CreateLogger<T>();
            return logger;
        }

        private static JsonSerializerSettings GetJsonSerializerSettings(IScorerFactory scorerFactory)
        {
            #if DEBUG
            return GetDebugJsonSerializerSettings(scorerFactory);
            #endif
            
            return GetProdJsonSerializerSettings(scorerFactory);
        }

        private static JsonSerializerSettings GetDebugJsonSerializerSettings(IScorerFactory scorerFactory)
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Include,
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Converters = new List<JsonConverter> { new StringEnumConverter(), new TruncatingDoubleConverter(), new ScorerConverter(scorerFactory) },
            };
        }

        private static JsonSerializerSettings GetProdJsonSerializerSettings(IScorerFactory scorerFactory)
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Converters = new List<JsonConverter> { new StringEnumConverter(), new TruncatingDoubleConverter(), new ScorerConverter(scorerFactory) },
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
            _logger.LogInformation("SIGTERM received. Shutting down");
            
            var stopwatch = Stopwatch.StartNew();
            _cts.Cancel();
            stopwatch.Stop();

            _logger.LogInformation($"Daemon stopped in {stopwatch.ElapsedMilliseconds:N0}ms");
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
