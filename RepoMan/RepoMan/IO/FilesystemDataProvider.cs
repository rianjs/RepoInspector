using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RepoMan.Analysis;
using RepoMan.Records;
using RepoMan.Repository;

namespace RepoMan.IO
{
    public class FilesystemDataProvider :
        IPullRequestCacheManager,
        IAnalysisManager
    {
        private readonly IFilesystem _fs;
        private readonly string _cacheParentDirectory;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="pathToCache">The directory that contains the cache files</param>
        /// <param name="jsonSerializerSettings"></param>
        public FilesystemDataProvider(IFilesystem fs, string pathToCache, JsonSerializerSettings jsonSerializerSettings)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _cacheParentDirectory = string.IsNullOrWhiteSpace(pathToCache)
                ? throw new ArgumentNullException(nameof(pathToCache))
                : pathToCache;
            _jsonSerializerSettings = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
        }

        public async ValueTask SaveAsync(IList<PullRequestDetails> prDetails, string repoOwner, string repoName)
        {
            var path = GetPullRequestDetailsPath(repoOwner, repoName);
            var json = JsonConvert.SerializeObject(prDetails, _jsonSerializerSettings);
            await _fs.FileWriteAllTextAsync(path, json);
        }

        public async ValueTask<IList<PullRequestDetails>> LoadAsync(string repoOwner, string repoName)
        {
            var path = GetPullRequestDetailsPath(repoOwner, repoName);
            var json = await _fs.FileReadAllTextAsync(path);
            return JsonConvert.DeserializeObject<List<PullRequestDetails>>(json, _jsonSerializerSettings);
        }

        private string GetPullRequestDetailsPath(string repoOwner, string repoName)
            => Path.Combine(_cacheParentDirectory, $"{repoOwner}-{repoName}-prs.json");

        public async ValueTask SaveAsync(string repoOwner, string repoName, DateTime utcTimestamp, RepositoryMetrics repoAnalysis)
        {
            if (string.IsNullOrWhiteSpace(repoOwner))
            {
                throw new ArgumentNullException(nameof(repoOwner));
            }

            if (string.IsNullOrWhiteSpace(repoName))
            {
                throw new ArgumentNullException(nameof(repoName));
            }

            if (utcTimestamp.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException($"UTC timestamp is not UTC, it is {utcTimestamp.Kind}");
            }

            if (repoAnalysis is null)
            {
                throw new ArgumentNullException(nameof(repoAnalysis));
            }
            
            var path = BuildFullPathToFile(repoOwner, repoName, utcTimestamp);
            var json = JsonConvert.SerializeObject(repoAnalysis, _jsonSerializerSettings);
            await _fs.FileWriteAllTextAsync(path, json);
        }

        public async ValueTask<RepositoryMetrics> LoadAsync(string repoOwner, string repoName, DateTime utcTimestamp)
        {
            if (string.IsNullOrWhiteSpace(repoOwner))
            {
                throw new ArgumentNullException(nameof(repoOwner));
            }

            if (string.IsNullOrWhiteSpace(repoName))
            {
                throw new ArgumentNullException(nameof(repoName));
            }

            if (utcTimestamp.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException($"UTC timestamp is not UTC, it is {utcTimestamp.Kind}");
            }

            var path = BuildFullPathToFile(repoOwner, repoName, utcTimestamp);
            var json = await _fs.FileReadAllTextAsync(path);
            var snapshot = JsonConvert.DeserializeObject<RepositoryMetrics>(json, _jsonSerializerSettings);
            return snapshot;
        }

        public ValueTask<List<RepositoryMetrics>> LoadHistoryAsync(string repoOwner, string repoName)
        {
            throw new NotImplementedException();

            if (string.IsNullOrWhiteSpace(repoOwner))
            {
                throw new ArgumentNullException(nameof(repoOwner));
            }

            if (string.IsNullOrWhiteSpace(repoName))
            {
                throw new ArgumentNullException(nameof(repoName));
            }
        }

        private string BuildFullPathToFile(string repoOwner, string repoName, DateTime utcTimestamp)
        {
            var normalizedTimestamp = $"{utcTimestamp:u}"
                .Replace(" ", "T")
                .Replace(":", "-");
            return Path.Combine(_cacheParentDirectory, $"{repoOwner}-{repoName}-{normalizedTimestamp}-analysis.json");
        }
    }
}