using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RepoMan.Analysis;
using RepoMan.IO;
using RepoMan.Records;
using Microsoft.Extensions.Logging;

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
                ?? new List<MetricSnapshot>();

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
            var toBeSaved = new List<MetricSnapshot>();
            if (previouslyComputedButHasChanged.Any())
            {
                var now = clock.DateTimeOffsetUtcNow();

                foreach (var originalMetric in previouslyComputedButHasChanged)
                {
                    var prMetrics = originalMetric.PullRequestMetrics
                        .Select(prMetric => prMetric.Value.Number)
                        .Select(nbr => cachedPullRequestByNumber[nbr])
                        .Select(prAnalyzer.CalculatePullRequestMetrics)
                        .ToList();
                    var replacements = repoAnalyzer.CalculateRepositoryMetricsOverTime(prMetrics);
                    
                    foreach (var replacement in replacements)
                    {
                        replacement.CreatedAt = originalMetric.CreatedAt;
                        replacement.UpdatedAt = now;
                        replacement.Scorers = prAnalyzer.Scorers.ToHashSet();
                        replacement.Owner = repoManager.RepoOwner;
                        replacement.Name = repoManager.RepoName;
                        replacement.Url = repoManager.RepoUrl;
                    }
                    toBeSaved.AddRange(replacements);
                }
            }

            if (cachedButNotAnalyzed.Any())
            {
                // 2b) Analyze this new stuff
                var now = clock.DateTimeOffsetUtcNow();
                var newPullRequestMetrics = cachedButNotAnalyzed
                    .Select(nbr => cachedPullRequestByNumber[nbr])
                    .Select(prAnalyzer.CalculatePullRequestMetrics)
                    .ToList();
                var newRepoMetrics = repoAnalyzer.CalculateRepositoryMetricsOverTime(newPullRequestMetrics);
                var prAnalyzers = prAnalyzer.Scorers.ToHashSet();
                foreach (var newRepoMetric in newRepoMetrics)
                {
                    newRepoMetric.CreatedAt = now;
                    newRepoMetric.UpdatedAt = now;
                    newRepoMetric.Scorers = prAnalyzers;
                    newRepoMetric.Owner = repoManager.RepoOwner;
                    newRepoMetric.Name = repoManager.RepoName;
                    newRepoMetric.Url = repoManager.RepoUrl;
                }
                
                toBeSaved.AddRange(newRepoMetrics);
            }
            
            // 1c) Overwrite the old metric snapshots
            // 2c) Write down the new snapshot
            await analysisManager.SaveAsync(toBeSaved);
            return worker;
        }

        public async Task ExecuteAsync()
        {
            var now = _clock.DateTimeOffsetUtcNow();
            _logger.LogInformation($"{Name} work loop starting");
            var timer = Stopwatch.StartNew();
            
            var newPrs = await _repoManager.RefreshFromUpstreamAsync(ItemState.Closed);
            if (!newPrs.Any())
            {
                _logger.LogInformation($"{Name} has no pull requests analyze.");
                return;
            }

            _logger.LogInformation($"{Name} comment analysis starting for {newPrs.Count:N0} pull requests");
            var analysisTimer = Stopwatch.StartNew();
            var prAnalysis = newPrs
                .Select(pr => _prAnalyzer.CalculatePullRequestMetrics(pr))
                .ToList();
            analysisTimer.Stop();
            _logger.LogInformation($"{Name} comment analysis completed for {newPrs.Count:N0} pull requests in {analysisTimer.Elapsed.ToMicroseconds():N0} microseconds");
            
            _logger.LogInformation($"{Name} repository analysis starting for {newPrs.Count:N0} pull requests");
            analysisTimer = Stopwatch.StartNew();

            var repoSnapshots = _repoAnalyzer.CalculateRepositoryMetricsOverTime(prAnalysis);
            foreach (var repoAnalysis in repoSnapshots)
            {
                repoAnalysis.Owner = _repoManager.RepoOwner;
                repoAnalysis.Name = _repoManager.RepoName;
                repoAnalysis.Url = _repoManager.RepoUrl;
                repoAnalysis.CreatedAt = now;
                repoAnalysis.UpdatedAt = now;
                repoAnalysis.Scorers = _prAnalyzer.Scorers.ToHashSet();
            }
            analysisTimer.Stop();
            _logger.LogInformation($"{Name} repository analysis completed for {newPrs.Count:N0} pull requests in {analysisTimer.Elapsed.ToMicroseconds():N0} microseconds");

            await _analysisManager.SaveAsync(repoSnapshots);

            timer.Stop();
            _logger.LogInformation($"{Name} work loop completed in {timer.ElapsedMilliseconds:N0}ms");
        }
    }
}
