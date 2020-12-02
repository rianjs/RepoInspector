using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RepoMan.Records;

namespace RepoMan.IO
{
    public class FilesystemDataProvider :
        IPullRequestCacheManager,
        IAnalysisManager
    {
        private readonly IFilesystem _fs;
        private readonly string _cacheParentDirectory;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private const string _pullRequestDetails = "pull-requests.json";
        private const string _analysisSuffix = "-analysis.json";

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

        public async ValueTask SaveAsync(IList<PullRequest> prDetails, string repoOwner, string repoName)
        {
            var parentDirectory = GetPathToRepoDataFiles(repoOwner, repoName);
            var path = Path.Combine(parentDirectory, _pullRequestDetails);
            var json = JsonConvert.SerializeObject(prDetails, _jsonSerializerSettings);
            _fs.DirectoryCreateDirectory(parentDirectory);
            await _fs.FileWriteAllTextAsync(path, json);
        }

        public async ValueTask<IList<PullRequest>> LoadAsync(string repoOwner, string repoName)
        {
            var path = Path.Combine(GetPathToRepoDataFiles(repoOwner, repoName), _pullRequestDetails);
            var json = await _fs.FileReadAllTextAsync(path);
            var initialized = JsonConvert.DeserializeObject<List<PullRequest>>(json, _jsonSerializerSettings);
            return initialized;
        }
        
        private string GetPathToRepoDataFiles(string repoOwner, string repoName)
            => Path.Combine(_cacheParentDirectory, repoOwner, repoName);

        public async Task SaveAsync(RepositoryMetrics repoAnalysis)
        {
            if (repoAnalysis is null)
            {
                throw new ArgumentNullException(nameof(repoAnalysis));
            }
            
            var parentDirectory = GetPathToRepoDataFiles(repoAnalysis.Owner, repoAnalysis.Name);
            _fs.DirectoryCreateDirectory(parentDirectory);

            var normalizedTimestamp = GetNormalizedTimestamp(repoAnalysis.CreatedAt);
            var fullPath = Path.Combine(parentDirectory, $"{normalizedTimestamp}{_analysisSuffix}");
            var json = JsonConvert.SerializeObject(repoAnalysis, _jsonSerializerSettings);
            await _fs.FileWriteAllTextAsync(fullPath, json);
        }

        public async ValueTask<RepositoryMetrics> LoadAsync(string repoOwner, string repoName, DateTimeOffset timestamp)
        {
            if (string.IsNullOrWhiteSpace(repoOwner))
            {
                throw new ArgumentNullException(nameof(repoOwner));
            }

            if (string.IsNullOrWhiteSpace(repoName))
            {
                throw new ArgumentNullException(nameof(repoName));
            }

            var parentDirectory = GetPathToRepoDataFiles(repoOwner, repoName);
            var normalizedTimestamp = GetNormalizedTimestamp(timestamp);
            var fullPath = Path.Combine(parentDirectory, $"{normalizedTimestamp}{_analysisSuffix}");
            var json = await _fs.FileReadAllTextAsync(fullPath);
            var snapshot = JsonConvert.DeserializeObject<RepositoryMetrics>(json, _jsonSerializerSettings);
            return snapshot;
        }

        private string GetNormalizedTimestamp(DateTimeOffset timestamp)
        {
            var asUtc = timestamp.UtcDateTime;
            return $"{asUtc:u}".Replace(" ", "T").Replace(":", "-");
        }

        public async ValueTask<List<RepositoryMetrics>> LoadHistoryAsync(string repoOwner, string repoName)
        {
            if (string.IsNullOrWhiteSpace(repoOwner))
            {
                throw new ArgumentNullException(nameof(repoOwner));
            }

            if (string.IsNullOrWhiteSpace(repoName))
            {
                throw new ArgumentNullException(nameof(repoName));
            }
            
            const string searchPattern = "*" + _analysisSuffix;
            var searchPath = GetPathToRepoDataFiles(repoOwner, repoName);
            var matches = _fs.DirectoryGetFiles(searchPath, searchPattern);

            var readMatches = matches
                .Select(async p =>
                    new {
                        Path = p,
                        Contents = await _fs.FileReadAllTextAsync(p),
                    })
                .ToList();
            await Task.WhenAll(readMatches);

            var deserialized = readMatches
                .Select(t => t.Result)
                .Select(kvp => new
                {
                    kvp.Path,
                    Metrics = JsonConvert.DeserializeObject<RepositoryMetrics>(kvp.Contents, _jsonSerializerSettings),
                })
                .ToDictionary(kvp => kvp.Path, kvp => kvp.Metrics);

            return deserialized.Values.ToList();
        }
    }
}