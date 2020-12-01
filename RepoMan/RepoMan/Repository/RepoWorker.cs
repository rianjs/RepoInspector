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
            IPullRequestAnalyzer prAnalyzer,
            IRepositoryAnalyzer repoAnalyzer,
            IAnalysisManager analysisManager,
            IClock clock,
            ILogger logger)
        {
            _repoManager = repoManager ?? throw new ArgumentNullException(nameof(repoManager));
            Name = $"{repoManager.RepoOwner}:{repoManager.RepoName}";
            _prAnalyzer = prAnalyzer ?? throw new ArgumentNullException(nameof(prAnalyzer));
            _repoAnalyzer = repoAnalyzer ?? throw new ArgumentNullException(nameof(repoAnalyzer));
            _analysisManager = analysisManager ?? throw new ArgumentNullException(nameof(analysisManager));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        
        public async Task ExecuteAsync()
        {
            _logger.Information($"{Name} work loop starting");
            var timer = Stopwatch.StartNew();
            
            var newPrs = await _repoManager.RefreshFromUpstreamAsync(ItemStateFilter.Closed);
            // Read the analysis files -- if the PR isn't in the collective set, it hasn't been analyzed

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
