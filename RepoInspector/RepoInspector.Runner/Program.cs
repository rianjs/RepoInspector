using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
    internal class Program
    {
        private static readonly CancellationTokenSource _cts = new();

        private static async Task Main()
        {
            string configPath = Environment.GetEnvironmentVariable("REPO_INSPECTOR_CONFIG_PATH") ?? Path.Combine(Environment.CurrentDirectory, "..", "scratch", "repoman-config.json");
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile(configPath, false, true)
                .Build();

            var serviceCollection = new ServiceCollection();

            // These are separate from the regular service collection setup, because they don't chain cleanly without calling `.Services`, which is...hinky.
            serviceCollection.AddOptions<PullRequestConstants>(RepositoryKind.GitHub.ToString())
                .Bind(configuration.GetSection("PRConstants:GitHub"))
                .ValidateDataAnnotations();

            serviceCollection.AddOptions<PullRequestConstants>(RepositoryKind.BitBucketCloud.ToString())
                .Bind(configuration.GetSection("PRConstants:BitBucketCloud"))
                .ValidateDataAnnotations();

            // Bind the repo inspector options, and canonicalize the cache path and scratch directory.
            serviceCollection.AddOptions<RepoInspectorOptions>()
                .Bind(configuration.GetSection("Options"))
                .ValidateDataAnnotations()
                .Validate(rio => rio.DenialOfServiceBuffer > TimeSpan.Zero, "Denial of service buffer time must be greater than zero.")
                .Validate(rio => rio.LoopDelay > TimeSpan.Zero, "Loop delay time must be greater than zero.")
                .Validate(
                    rio => rio.HttpConnectionLifetime > TimeSpan.Zero && rio.HttpConnectionLifetime < TimeSpan.FromMinutes(10),
                    "HTTP connection lifetime must be greater than zero and less than 10 minutes."
                ).PostConfigure(rio =>
                {
                    rio.CachePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, rio.CachePath));
                    rio.ScratchDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, rio.ScratchDirectory));
                });

            serviceCollection.AddLogging(logBuilder =>
                {
                    logBuilder.AddFilter("Microsoft", LogLevel.Warning)
                        .AddFilter("System", LogLevel.Warning)
                        .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                        .AddConsole();
                }).AddTransient<GitHubApprovalAnalyzer>()
                .AddTransient<BitBucketApprovalAnalyzer>()
                // Reusable building blocks
                .AddSingleton<IFilesystem, Filesystem>()
                .AddSingleton<IClock, Clock>()
                .AddSingleton<IWordCounter, WordCounter>()
                .AddSingleton<INormalizer, HtmlCommentStripper>()
                .AddSingleton(sp => GetKnownScorers(
                    sp.GetRequiredService<GitHubApprovalAnalyzer>(),
                    sp.GetRequiredService<IWordCounter>()))
                .AddSingleton<ScorerConverter>()
                .AddSingleton(sp => GetJsonSerializerSettings(sp.GetRequiredService<IScorerFactory>()))
                .AddSingleton<IPullRequestCacheManager, FilesystemDataProvider>()
                .AddSingleton<IAnalysisManager, FilesystemDataProvider>()
                // CommentScorers
                .AddSingleton<Scorer, UrlScorer>()
                .AddSingleton<Scorer, CodeFenceScorer>()
                .AddSingleton<Scorer, CodeFragmentScorer>()
                .AddSingleton<Scorer, GitHubIssueLinkScorer>()
                // PullRequestScorers
                .AddSingleton<Scorer, WordCountScorer>()
                .AddSingleton<Scorer, BusinessDaysScorer>()
                .AddSingleton<Scorer, UniqueCommenterScorer>()
                .AddSingleton<Scorer, CommentCountScorer>()
                .AddSingleton<Scorer, ApprovalScorer>()
                // Roll it all up into the orchestrators and factories...
                .AddSingleton<IPullRequestAnalyzer, PullRequestAnalyzer>()
                .AddSingleton<IRepositoryAnalyzer, RepositoryAnalyzer>()
                .AddSingleton<GitHubPullRequestReaderFactory>()
                .AddSingleton<BitBucketCloudPullRequestReaderFactory>()
                .AddSingleton<IPullRequestReaderFactory, PullRequestReaderFactory>()
                .AddSingleton<IRepoManagerFactory, RepoManagerFactory>();

            var serviceProvider = serviceCollection.BuildServiceProvider();



            // FUTURE: Consider implementing an UpgradeAsync method that can do one-time data transformations/updates on cached data

            var repos = configuration.GetSection("WatchedRepositories").Get<List<WatchedRepository>>();
            var repoManagerFactory = serviceProvider.GetRequiredService<IRepoManagerFactory>();
            var repoManagers = repos
                .Select(r => repoManagerFactory.GetManagerAsync(r, false))
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
                    serviceProvider.GetRequiredService<ILogger<RepoWorker>>()))
                .ToList();
            await Task.WhenAll(repoWorkerInitialization);

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var options = serviceProvider.GetRequiredService<IOptions<RepoInspectorOptions>>();
            var loopServices = repoWorkerInitialization
                .Select(rwi => rwi.Result)
                .Select(rw => new LoopService(rw, options.Value.LoopDelay, _cts, logger))
                .Select(l => l.LoopAsync())
                .ToList();
            await Task.WhenAll(loopServices);
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
            return new()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Include,
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Converters = new List<JsonConverter> { new StringEnumConverter(), new TruncatingDoubleConverter(), new ScorerConverter(scorerFactory) }
            };
        }

        private static JsonSerializerSettings GetProdJsonSerializerSettings(IScorerFactory scorerFactory)
        {
            return new()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.None,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Converters = new List<JsonConverter> { new StringEnumConverter(), new TruncatingDoubleConverter(), new ScorerConverter(scorerFactory) }
            };
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
            if (s == typeof(ApprovalScorer)) return Activator.CreateInstance(typeof(ApprovalScorer), approvalAnalyzer);

            if (s == typeof(CommentCountScorer)) return Activator.CreateInstance(typeof(CommentCountScorer), wc);

            if (s == typeof(WordCountScorer)) return Activator.CreateInstance(typeof(WordCountScorer), wc);

            return Activator.CreateInstance(s);
        }
    }
}
