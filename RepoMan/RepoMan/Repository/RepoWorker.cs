using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Octokit;
using RepoMan.Analysis;
using RepoMan.Analysis.ApprovalAnalyzers;
using RepoMan.Analysis.Scoring;
using Serilog;

namespace RepoMan.Repository
{
    class RepoWorker :
        IWorker
    {
        private readonly string _fullName;
        private readonly IRepoManager _repo;
        private readonly IPullRequestAnalyzer _prAnalyzer;
        private readonly IRepositoryAnalyzer _repoAnalyzer;
        private readonly ILogger _logger;
        
        public RepoWorker(
            IRepoManager repo,
            IPullRequestAnalyzer prAnalyzer,
            IRepositoryAnalyzer repoAnalyzer,
            ILogger logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _fullName = $"{repo.RepoOwner}:{repo.RepoName}";
            _prAnalyzer = prAnalyzer ?? throw new ArgumentNullException(nameof(prAnalyzer));
            _repoAnalyzer = repoAnalyzer ?? throw new ArgumentNullException(nameof(repoAnalyzer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ExecuteAsync()
        {
            _logger.Information($"{_fullName} work loop starting");
            var timer = Stopwatch.StartNew();
            
            // TODO: Refresh from upstream first
            await _repo.RefreshFromUpstreamAsync(ItemStateFilter.Closed);
            var pullRequestSnapshots = await _repo.GetPullRequestsAsync();

            _logger.Information($"{_fullName} comment analysis starting for {pullRequestSnapshots.Count:N0} pull requests");
            var analysisTimer = Stopwatch.StartNew();
            
            // TODO: Calculate the PR snapshots
            
            var singularPrSnapshots = pullRequestSnapshots
                .Select(pr => _prAnalyzer.CalculateCommentStatistics(pr))
                .ToList();
            analysisTimer.Stop();
            _logger.Information($"{_fullName} comment analysis completed for {pullRequestSnapshots.Count:N0} pull requests in {analysisTimer.Elapsed.ToMicroseconds():N0} microseconds");
            
            // TODO: Do something with the comment statistics
            
            _logger.Information($"{_fullName} repository analysis starting for {pullRequestSnapshots.Count:N0} pull requests");
            analysisTimer = Stopwatch.StartNew();
            
            // TODO: Calculate the aggregate statistics
            
            var repoHealth = _repoAnalyzer.CalculateRepositoryHealthStatistics(singularPrSnapshots);
            analysisTimer.Stop();
            _logger.Information($"{_fullName} repository analysis completed for {pullRequestSnapshots.Count:N0} pull requests in {analysisTimer.Elapsed.ToMicroseconds():N0} microseconds");

            // TODO: Do something with the repo health snapshot
            
            
            timer.Stop();
            _logger.Information($"{_fullName} work loop completed in {timer.ElapsedMilliseconds:N0}ms");
        }
    }
}
