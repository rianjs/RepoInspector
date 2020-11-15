using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RepoMan.Filesystem;
using Serilog;

namespace RepoMan.RepoHistory
{
    /// <summary>
    /// Handles the cache management (both reading and writing) associated with historical repositories, since querying the GitHub API is slow, and rate-limited 
    /// </summary>
    public class RepoHistoryManager :
        IRepoHistoryManager
    {
        private readonly IFilesystem _fs;
        private readonly string _cachePath;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly SemaphoreSlim _byNumberLock = new SemaphoreSlim(1, 1);
        private readonly IRepoPullRequestReader _prReader;
        private readonly Dictionary<int, PullRequestDetails> _byNumber;
        private readonly ILogger _logger;

        private RepoHistoryManager(
            IFilesystem fs,
            string cachePath,
            IRepoPullRequestReader prReader,
            JsonSerializerSettings jsonSerializerSettings,
            Dictionary<int, PullRequestDetails> byNumber,
            ILogger logger)
        {
            _fs = fs ;
            _cachePath = cachePath;
            _jsonSerializerSettings = jsonSerializerSettings;
            _logger = logger;
            _byNumber = byNumber;
        }

        public static async Task<RepoHistoryManager> InitializeAsync(
            IFilesystem fs,
            string cachePath,
            IRepoPullRequestReader prReader,
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
            
            return new RepoHistoryManager(fs, cachePath, prReader, jsonSerializerSettings, byNumber, logger);
        }

        /// <summary>
        /// Sometimes it's not possible to fully interrogate the commit graph
        /// </summary>
        /// <returns></returns>
        public async Task<ICollection<PullRequestDetails>> GetUnfinishedPullRequestsAsync()
        {
            try
            {
                await _byNumberLock.WaitAsync();
                var unfinished = _byNumber.Values
                    .Where(pr => !pr.IsFullyInterrogated)
                    .ToList();
                return unfinished;
            }
            finally
            {
                _byNumberLock.Release();
            }
        }

        /// <summary>
        /// Async-await and thread-safe update for recently-imported pull requests, and persists the updated collection to its home. The import is naive: if you
        /// supply a bad collection to be imported, your cache will be fried with bad data. If you supply incomplete pull requests, they will overwrite completed
        /// pull requests with no warning.
        /// </summary>
        /// <param name="imports"></param>
        /// <returns></returns>
        public async Task ImportPullRequestsAsync(ICollection<PullRequestDetails> imports)
        {
            _logger.Information($"Attempting to import {imports.Count:N0} new pull requests");
            
            var incompleteImports = imports
                .Where(pr => !pr.IsFullyInterrogated)
                .ToList();
            
            var timer = Stopwatch.StartNew();

            try
            {
                await _byNumberLock.WaitAsync();
                foreach (var updatedPr in imports)
                {
                    _byNumber[updatedPr.Number] = updatedPr;
                }

                await SaveAsync();
            }
            catch (Exception e)
            {
                _logger.Error($"Something went wrong during the import process...");
                throw;
            }
            finally
            {
                timer.Stop();
                _byNumberLock.Release();
            }
            
            _logger.Information($"{imports.Count:N0} pull requests were imported and persisted to the cache data store in {timer.ElapsedMilliseconds:N0}ms");

            if (incompleteImports.Any())
            {
                await PopulateUnfinishedPullRequestsAsync();
            }
        }

        private async Task SaveAsync()
        {
            try
            {
                await _byNumberLock.WaitAsync();
                var json = JsonConvert.SerializeObject(_byNumber.Values, _jsonSerializerSettings);
                await _fs.FileReadAllTextAsync(json);
            }
            finally
            {
                _byNumberLock.Release();
            }
        }

        public async Task PopulateUnfinishedPullRequestsAsync(TimeSpan delay)
        {
            _logger.Information("Finding incomplete pull requests...");
            var unfinishedPrs = await GetUnfinishedPullRequestsAsync();
            _logger.Information($"{unfinishedPrs.Count:N0} pull requests with incomplete data found in the cache");
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

                await Task.Delay(delay);
            }

            if (!successfullyUpdatePrs.Any())
            {
                return;
            }
            
            _logger.Information($"Updating the long-term cache with {successfullyUpdatePrs.Count:N0} new entries");
            var timer = Stopwatch.StartNew();
            await ImportPullRequestsAsync(successfullyUpdatePrs);
            timer.Stop();
            _logger.Information($"{successfullyUpdatePrs.Count} new pull requests written to the cache");
        }

        public async Task<int> GetPullRequestCount()
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
    }
}