using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Octokit;
using RepoMan.Filesystem;
using RepoMan.PullRequest;
using Serilog;

namespace RepoMan.Repository
{
    /// <summary>
    /// Handles the cache management (both reading and writing) associated with historical repositories, since querying the GitHub API is slow, and rate-limited.
    /// Under the hood, many methods utilize SemaphoreSlim for read/write thread-safety, which is why many methods are async Tasks instead of values.
    /// </summary>
    public class RepoHistoryManager :
        IRepoHistoryManager
    {
        private readonly IFilesystem _fs;
        private readonly string _cachePath;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly SemaphoreSlim _byNumberLock = new SemaphoreSlim(1, 1);
        private readonly IRepoPullRequestReader _prReader;
        private readonly TimeSpan _dosBuffer;
        
        private readonly Dictionary<int, PullRequestDetails> _byNumber;
        private readonly ILogger _logger;

        private RepoHistoryManager(
            IFilesystem fs,
            string cachePath,
            IRepoPullRequestReader prReader,
            TimeSpan prApiDosBuffer,
            JsonSerializerSettings jsonSerializerSettings,
            Dictionary<int, PullRequestDetails> byNumber,
            ILogger logger)
        {
            _fs = fs;
            _cachePath = cachePath;
            _prReader = prReader;
            _dosBuffer = prApiDosBuffer;
            _jsonSerializerSettings = jsonSerializerSettings;
            _logger = logger;
            _byNumber = byNumber;
        }

        public static async Task<RepoHistoryManager> InitializeAsync(
            IFilesystem fs,
            string cachePath,
            IRepoPullRequestReader prReader,
            TimeSpan prApiDosBuffer,
            bool refreshFromUpstream,
            JsonSerializerSettings jsonSerializerSettings,
            ILogger logger)
        {
            if (fs is null)
            {
                throw new ArgumentNullException(nameof(fs));
            }

            if (string.IsNullOrWhiteSpace(cachePath))
            {
                throw new ArgumentNullException(nameof(cachePath));
            }

            if (prReader is null)
            {
                throw new ArgumentNullException(nameof(prReader));
            }
            
            if (prApiDosBuffer < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(prApiDosBuffer));
            }
            
            if (jsonSerializerSettings is null)
            {
                throw new ArgumentNullException(nameof(jsonSerializerSettings));
            }

            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            
            logger.Information("Initializing repository history manager cache");
            var timer = Stopwatch.StartNew();

            var prs = new List<PullRequestDetails>();
            try
            {
                logger.Information($"Reading the cache file: {cachePath}");
                var json = await fs.FileReadAllTextAsync(cachePath);
                prs = JsonConvert.DeserializeObject<List<PullRequestDetails>>(json, jsonSerializerSettings)
                      ?? new List<PullRequestDetails>();
            }
            catch (FileNotFoundException)
            {
                logger.Information("Cache file does not exist, therefore one will be created.");
            }
            var byNumber = prs.ToDictionary(pr => pr.Number);
            timer.Stop();
            logger.Information($"Initialized the cache with {byNumber.Count:N0} pull requests in {timer.ElapsedMilliseconds:N0}ms");
            
            var repoHistoryMgr = new RepoHistoryManager(fs, cachePath, prReader, prApiDosBuffer, jsonSerializerSettings, byNumber, logger);

            if (!refreshFromUpstream)
            {
                return repoHistoryMgr;
            }
            
            // If there's nothing in the cache, go look for stuff.
            await repoHistoryMgr.RefreshFromUpstreamAsync(ItemStateFilter.Closed);
            return repoHistoryMgr;
        }

        /// <summary>
        /// </summary>
        /// <param name="stateFilter"></param>
        /// <returns></returns>
        public async Task RefreshFromUpstreamAsync(ItemStateFilter stateFilter)
        {
            var prRootTask = _prReader.GetPullRequestsRootAsync(stateFilter);
            var unknownPrs = new List<PullRequestDetails>();            
            
            try
            {
                await _byNumberLock.WaitAsync();
                var prs = await prRootTask;
                var unknownPrsQuery = prs.Where(pr => !_byNumber.ContainsKey(pr.Number));
                unknownPrs.AddRange(unknownPrsQuery);
            }
            finally
            {
                _byNumberLock.Release();
            }

            var completedPrs = await PopulateCommentsAndApprovalsAsync(unknownPrs);

            if (completedPrs.Any())
            {
                await UpdateMemoryCacheAsync(completedPrs);
                await PersistCacheAsync();
            }
        }

        /// <summary>
        /// Async-await and thread-safe update for recently-imported pull requests, and persists the updated collection to its home. The import is naive: if you
        /// supply a bad collection to be imported, your cache will be fried with bad data. If you supply incomplete pull requests, they will overwrite completed
        /// pull requests with no warning.
        /// </summary>
        /// <param name="prRoots"></param>
        /// <returns></returns>
        private async ValueTask UpdateMemoryCacheAsync(List<PullRequestDetails> prRoots)
        {
            if (!prRoots.Any())
            {
                return;
            }
            
            try
            {
                await _byNumberLock.WaitAsync();
                foreach (var updatedPr in prRoots)
                {
                    _byNumber[updatedPr.Number] = updatedPr;
                }
            }
            finally
            {
                _byNumberLock.Release();
            }
        }

        private async ValueTask PersistCacheAsync()
        {
            var updates = new List<PullRequestDetails>(_byNumber.Count);    // Dirty read is OK for minor GC optimization
            try
            {
                await _byNumberLock.WaitAsync();
                updates.AddRange(_byNumber.Values);
            }
            finally
            {
                _byNumberLock.Release();
            }
            
            var json = JsonConvert.SerializeObject(updates, _jsonSerializerSettings);
            await _fs.FileWriteAllTextAsync(_cachePath, json);
        }

        /// <summary>
        /// Deep queries the git API to retrieve all comments, approvals, etc. for each of the specified pull requests to its data store. If the git API starts
        /// rejecting queries (due to rate limits or whatever), the pull requests that have had their comments approval and comment data fully populated are
        /// returned.
        /// </summary>
        /// <returns>The collection of fully-updated pull requests</returns>
        private async ValueTask<List<PullRequestDetails>> PopulateCommentsAndApprovalsAsync(List<PullRequestDetails> unfinishedPrs)
        {
            _logger.Information($"{unfinishedPrs.Count:N0} pull requests with incomplete data to be populated");
            var successfullyUpdatePrs = new List<PullRequestDetails>(unfinishedPrs.Count);
            
            foreach (var pr in unfinishedPrs)
            {
                _logger.Information($"Updating {pr.Number}");
                var loopTimer = Stopwatch.StartNew();
                var succeeded = await _prReader.TryFillCommentGraphAsync(pr);
                loopTimer.Stop();
                
                if (!succeeded)
                {
                    _logger.Information($"Pull request {pr.Number} update failed in {loopTimer.ElapsedMilliseconds:N0}");
                    break;
                }
                _logger.Information($"Pull request {pr.Number} update succeeded in {loopTimer.ElapsedMilliseconds:N0}");
                
                successfullyUpdatePrs.Add(pr);

                await Task.Delay(_dosBuffer);
            }

            return successfullyUpdatePrs;
        }

        public async ValueTask<int> GetPullRequestCount()
        {
            try
            {
                await _byNumberLock.WaitAsync();
                return _byNumber.Count;
            }
            finally
            {
                _byNumberLock.Release();
            }
        }

        public async ValueTask<IList<Comment>> GetAllCommentsForRepo()
        {
            try
            {
                await _byNumberLock.WaitAsync();
                var commitComments = _byNumber.SelectMany(pr => pr.Value.CommitComments);
                var diffComments = _byNumber.SelectMany(pr => pr.Value.DiffComments);
                var reviewComments = _byNumber.SelectMany(pr => pr.Value.ReviewComments);
                return commitComments.Concat(diffComments).Concat(reviewComments).ToList();
            }
            finally
            {
                _byNumberLock.Release();
            }
        }
        
        /// <summary>
        /// </summary>
        /// <param name="prNumber"></param>
        /// <returns>Null if the pull request number is not found</returns>
        public async ValueTask<PullRequestDetails> GetPullRequestByNumber(int prNumber)
        {
            try
            {
                await _byNumberLock.WaitAsync();
                return _byNumber.ContainsKey(prNumber)
                    ? _byNumber[prNumber]
                    : null;
            }
            finally
            {
                _byNumberLock.Release();
            }
        }

        /// <summary>
        /// Thread-safe, lazy, non-copy read from the in-memory cache
        /// </summary>
        /// <returns></returns>
        public async IAsyncEnumerable<PullRequestDetails> GetPullRequestsAsync()
        {
            try
            {
                await _byNumberLock.WaitAsync();
                foreach (var prDetails in _byNumber.Values)
                {
                    yield return prDetails;
                }
            }
            finally
            {
                _byNumberLock.Release();
            }
        }
    }
}