using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
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
            _repoManager = repoManager;
            Name = workerName;
            _prAnalyzer = prAnalyzer;
            _repoAnalyzer = repoAnalyzer;
            _analysisManager = analysisManager;
            _clock = clock;
            _logger = logger;
        }

        public async Task InitializeAsync(IRepoManager repoManager,
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
            
            // See what PRs have been analyzed by loading the existing analysis files, and seeing if the set of analyzers has changed.
            // If so, recompute the world, but save the data back into the original locations
            var existingMetrics = new HashSet<RepositoryMetrics>(await analysisManager.LoadHistoryAsync(repoManager.RepoOwner, repoManager.RepoName));

            var knownPrs = await _repoManager.GetCachedPullRequestsAsync();
            var recomputedPrMetrics = knownPrs
                .Select(prAnalyzer.CalculatePullRequestMetrics)
                .ToHashSet();
            
            // var newMetrics = existingMetrics
            //     .Select()
            
            // Recompute everything...

        }
        
        public async Task ExecuteAsync()
        {
            _logger.Information($"{Name} work loop starting");
            var timer = Stopwatch.StartNew();
            
            var newPrs = await _repoManager.RefreshFromUpstreamAsync(ItemStateFilter.Closed);

            _logger.Information($"{Name} comment analysis starting for {newPrs.Count:N0} pull requests");
            var analysisTimer = Stopwatch.StartNew();
            var prAnalysis = newPrs
                .Select(_prAnalyzer.CalculatePullRequestMetrics)
                .ToList();
            analysisTimer.Stop();
            _logger.Information($"{Name} comment analysis completed for {newPrs.Count:N0} pull requests in {analysisTimer.Elapsed.ToMicroseconds():N0} microseconds");
            
            _logger.Information($"{Name} repository analysis starting for {newPrs.Count:N0} pull requests");
            analysisTimer = Stopwatch.StartNew();
            var repoAnalysis = _repoAnalyzer.CalculateRepositoryMetrics(prAnalysis);
            analysisTimer.Stop();
            _logger.Information($"{Name} repository analysis completed for {newPrs.Count:N0} pull requests in {analysisTimer.Elapsed.ToMicroseconds():N0} microseconds");

            if (repoAnalysis is object)
            {
                await _analysisManager.SaveAsync(_repoManager.RepoOwner, _repoManager.RepoName, _clock.DateTimeUtcNow(), repoAnalysis);
            }

            timer.Stop();
            _logger.Information($"{Name} work loop completed in {timer.ElapsedMilliseconds:N0}ms");
        }
    }
}
