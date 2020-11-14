using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RepoMan.Filesystem;

namespace RepoMan
{
    /// <summary>
    /// Handles the cache management (both reading and writing) associated with historical repositories, since querying the GitHub API is slow, and rate-limited 
    /// </summary>
    public class FilesystemRepoHistoryManager
    {
        private readonly IFilesystem _fs;
        private readonly string _cachePath;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly SemaphoreSlim _byNumberLock = new SemaphoreSlim(1, 1);
        private readonly Dictionary<int, PullRequestDetails> _prLookup;

        private FilesystemRepoHistoryManager(
            IFilesystem fs,
            string cachePath,
            JsonSerializerSettings jsonSerializerSettings,
            IEnumerable<PullRequestDetails> prDetails)
        {
            _fs = fs ;
            _cachePath = cachePath;
            _jsonSerializerSettings = jsonSerializerSettings;
            _prLookup = prDetails.ToDictionary(pr => pr.Number);
        }

        public static async Task<FilesystemRepoHistoryManager> CreateAsync(
            IFilesystem fs,
            string cachePath,
            JsonSerializerSettings jsonSerializerSettings)
        {
            if (fs is null)
            {
                throw new ArgumentNullException(nameof(fs));
            }

            if (string.IsNullOrWhiteSpace(cachePath))
            {
                throw new ArgumentNullException(nameof(cachePath));
            }

            if (jsonSerializerSettings is null)
            {
                throw new ArgumentNullException(nameof(jsonSerializerSettings));
            }
            
            var json = await fs.FileReadAllTextAsync(cachePath);
            var prs = JsonConvert.DeserializeObject<List<PullRequestDetails>>(json, jsonSerializerSettings);
            
            return new FilesystemRepoHistoryManager(fs, cachePath, jsonSerializerSettings, prs);
        }

        /// <summary>
        /// Sometimes it's not possible to fully interrogate the commit graph
        /// </summary>
        /// <returns></returns>
        public List<PullRequestDetails> GetUnfinishedPullRequests()
        {
            var unfinished = _prLookup.Values
                .Where(pr => !pr.IsFullyInterrogated)
                .ToList();
            return unfinished;
        }

        /// <summary>
        /// Async-await and thread-safe update for recently-imported pull requests. Persists the updated collection to its home.
        /// </summary>
        /// <param name="imports"></param>
        /// <returns></returns>
        public async Task ImportCompletedPullRequests(IEnumerable<PullRequestDetails> imports)
        {
            try
            {
                await _byNumberLock.WaitAsync();
                foreach (var updatedPr in imports.Where(pr => pr.IsFullyInterrogated))
                {
                    _prLookup[updatedPr.Number] = updatedPr;
                }

                var contents = _prLookup.Keys.ToList();
                var serialized = JsonConvert.SerializeObject(contents, _jsonSerializerSettings);
                await _fs.FileWriteAllTextAsync(_cachePath, serialized);
            }
            finally
            {
                _byNumberLock.Release();
            }
        }
    }
}