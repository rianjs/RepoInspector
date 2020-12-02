using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using RepoMan.Analysis;
using RepoMan.IO;
using RepoMan.Records;
using Serilog;

namespace RepoMan.Repository
{
    class RepoWorker :
        IWorker
    {
        public string Name { get; }
        private readonly IRepoManager _repoManager;
        private readonly IPullRequestAnalyzer _prAnalyzer;
        private readonly IRepositoryAnalyzer _repoAnalyzer;
        private readonly IAnalysisManager _analysisManager;
        private readonly IClock _clock;
        private readonly ILogger _logger;
        
        public RepoWorker(
            IRepoManager repoManager,
            string workerName,
            IPullRequestAnalyzer prAnalyzer,
            IRepositoryAnalyzer repoAnalyzer,
            IAnalysisManager analysisManager,
            IClock clock,
            ILogger logger)
        {
            _repoManager = repoManager ?? throw new ArgumentNullException(nameof(repoManager));
            Name = workerName;
            _prAnalyzer = prAnalyzer ?? throw new ArgumentNullException(nameof(prAnalyzer));
            _repoAnalyzer = repoAnalyzer ?? throw new ArgumentNullException(nameof(repoAnalyzer));
            _analysisManager = analysisManager ?? throw new ArgumentNullException(nameof(analysisManager));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public static async Task<RepoWorker> InitializeAsync(
            IRepoManager repoManager,
            IPullRequestAnalyzer prAnalyzer,
            IRepositoryAnalyzer repoAnalyzer,
            IAnalysisManager analysisManager,
            IClock clock,
            ILogger logger)
        {
            if (repoManager is null) throw new ArgumentNullException(nameof(repoManager));
            if (prAnalyzer is null) throw new ArgumentNullException(nameof(prAnalyzer));
            if (repoAnalyzer is null) throw new ArgumentNullException(nameof(repoAnalyzer));
            if (analysisManager is null) throw new ArgumentNullException(nameof(analysisManager));
            if (clock is null) throw new ArgumentNullException(nameof(clock));
            if (logger is null) throw new ArgumentNullException(nameof(logger));

            var name = $"{repoManager.RepoOwner}:{repoManager.RepoName}";
            
            // Analysis steps
            // 1a) Find metrics for where the set of analyzers has differed
            // 1b) Recompute those metrics
            // 1c) Overwrite the old metric snapshots
            // 2a) Find things known to the cache, but that haven't been analyzed
            // 2b) Analyze this new stuff
            // 2c) Write down the new snapshot
            
            var cachedPullRequestByNumber = (await repoManager.GetPullRequestsAsync())
                .ToDictionary(pr => pr.Number);
            
            var previousMetrics = (await analysisManager.LoadHistoryAsync(repoManager.RepoOwner, repoManager.RepoName))
                ?? new List<RepositoryMetrics>();

            var previousPullRequestMetricsByNumber = previousMetrics
                .SelectMany(m => m.PullRequestMetrics.Values)
                .ToDictionary(pr => pr.Number);
            
            // 1a) Find metrics for where the set of analyzers has differed
            var previouslyComputedButHasChanged = previousMetrics
                .Where(m => !m.Scorers.SetEquals(prAnalyzer.Scorers))
                .ToList();
            
            // 2a) Find things known to the cache, but that haven't been analyzed
            var cachedButNotAnalyzed = cachedPullRequestByNumber
                .Where(pr => !previousPullRequestMetricsByNumber.ContainsKey(pr.Key))
                .Select(pr => pr.Key)
                .ToList();
            
            var worker = new RepoWorker(repoManager, name, prAnalyzer, repoAnalyzer, analysisManager, clock, logger);

            var analysisToBeDone = previouslyComputedButHasChanged.Any() || cachedButNotAnalyzed.Any();
            if (!analysisToBeDone)
            {
                return worker;
            }
            
            // 1b) Recompute those metrics
            var toBeSaved = new List<RepositoryMetrics>();
            if (previouslyComputedButHasChanged.Any())
            {
                foreach (var originalMetric in previouslyComputedButHasChanged)
                {
                    var prMetrics = originalMetric.PullRequestMetrics
                        .Select(prMetric => prMetric.Value.Number)
                        .Select(nbr => cachedPullRequestByNumber[nbr])
                        .Select(prAnalyzer.CalculatePullRequestMetrics)
                        .ToList();
                    var replacement = repoAnalyzer.CalculateRepositoryMetrics(prMetrics);
                    replacement.CreatedAt = originalMetric.CreatedAt;
                    replacement.UpdatedAt = clock.DateTimeOffsetUtcNow();
                    replacement.Scorers = prAnalyzer.Scorers.ToHashSet();
                    replacement.Owner = repoManager.RepoOwner;
                    replacement.Name = repoManager.RepoName;
                    replacement.Url = repoManager.RepoUrl;
                    toBeSaved.Add(replacement);
                }
            }

            if (cachedButNotAnalyzed.Any())
            {
                // 2b) Analyze this new stuff
                var newPullRequestMetrics = cachedButNotAnalyzed
                    .Select(nbr => cachedPullRequestByNumber[nbr])
                    .Select(prAnalyzer.CalculatePullRequestMetrics)
                    .ToList();
                var newRepoMetric = repoAnalyzer.CalculateRepositoryMetrics(newPullRequestMetrics);
                var now = clock.DateTimeOffsetUtcNow();
                newRepoMetric.CreatedAt = now;    // Just in case there's drift
                newRepoMetric.UpdatedAt = now;
                newRepoMetric.Scorers = prAnalyzer.Scorers.ToHashSet();
                newRepoMetric.Owner = repoManager.RepoOwner;
                newRepoMetric.Name = repoManager.RepoName;
                newRepoMetric.Url = repoManager.RepoUrl;
                toBeSaved.Add(newRepoMetric);
            }
            
            // 1c) Overwrite the old metric snapshots
            // 2c) Write down the new snapshot
            var saveTasks = toBeSaved
                .Select(analysisManager.SaveAsync)
                .ToList();
            await Task.WhenAll(saveTasks);

            return worker;
        }

        public async Task ExecuteAsync()
        {
            var now = _clock.DateTimeOffsetUtcNow();
            _logger.Information($"{Name} work loop starting");
            var timer = Stopwatch.StartNew();
            
            var newPrs = await _repoManager.RefreshFromUpstreamAsync(ItemState.Closed);

            _logger.Information($"{Name} comment analysis starting for {newPrs.Count:N0} pull requests");
            var analysisTimer = Stopwatch.StartNew();
            var prAnalysis = newPrs
                .Select(pr => _prAnalyzer.CalculatePullRequestMetrics(pr))
                .ToList();
            analysisTimer.Stop();
            _logger.Information($"{Name} comment analysis completed for {newPrs.Count:N0} pull requests in {analysisTimer.Elapsed.ToMicroseconds():N0} microseconds");
            
            _logger.Information($"{Name} repository analysis starting for {newPrs.Count:N0} pull requests");
            analysisTimer = Stopwatch.StartNew();

            var repoAnalysis = _repoAnalyzer.CalculateRepositoryMetrics(prAnalysis);
            repoAnalysis.Owner = _repoManager.RepoOwner;
            repoAnalysis.Name = _repoManager.RepoName;
            repoAnalysis.Url = _repoManager.RepoUrl;
            repoAnalysis.CreatedAt = now;
            repoAnalysis.UpdatedAt = now;
            repoAnalysis.Scorers = _prAnalyzer.Scorers.ToHashSet();
            
            analysisTimer.Stop();
            _logger.Information($"{Name} repository analysis completed for {newPrs.Count:N0} pull requests in {analysisTimer.Elapsed.ToMicroseconds():N0} microseconds");

            await _analysisManager.SaveAsync(repoAnalysis);

            timer.Stop();
            _logger.Information($"{Name} work loop completed in {timer.ElapsedMilliseconds:N0}ms");
        }
    }
}
