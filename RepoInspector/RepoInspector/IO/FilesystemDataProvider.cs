using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RepoInspector.Records;

namespace RepoInspector.IO
{
    public class FilesystemDataProvider :
        IPullRequestCacheManager,
        IAnalysisManager
    {
        private const string PullRequestDetails = "pull-requests.json";
        private const string AnalysisSuffix = "-analysis.json";

        private readonly IFilesystem _fs;
        private readonly string _cacheParentDirectory;
        private readonly JsonSerializerSettings _jsonSerializerSettings;

        public FilesystemDataProvider(IFilesystem fs, IOptionsSnapshot<RepoInspectorOptions> optionsSnapshot, JsonSerializerSettings jsonSerializerSettings)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _cacheParentDirectory = optionsSnapshot.Value.CachePath;
            _jsonSerializerSettings = jsonSerializerSettings ?? throw new ArgumentNullException(nameof(jsonSerializerSettings));
        }

        public async ValueTask SaveAsync(IList<PullRequest> prDetails, string repoOwner, string repoName)
        {
            var parentDirectory = GetPathToRepoDataFiles(repoOwner, repoName);
            var path = Path.Combine(parentDirectory, PullRequestDetails);
            var json = JsonConvert.SerializeObject(prDetails, _jsonSerializerSettings);
            _fs.DirectoryCreateDirectory(parentDirectory);
            await _fs.FileWriteAllTextAsync(path, json);
        }

        public async ValueTask<IList<PullRequest>> LoadAsync(string repoOwner, string repoName)
        {
            var path = Path.Combine(GetPathToRepoDataFiles(repoOwner, repoName), PullRequestDetails);
            var json = await _fs.FileReadAllTextAsync(path);
            var initialized = JsonConvert.DeserializeObject<List<PullRequest>>(json, _jsonSerializerSettings);
            return initialized;
        }
        
        private string GetPathToRepoDataFiles(string repoOwner, string repoName)
            => Path.Combine(_cacheParentDirectory, repoOwner, repoName);

        public async Task SaveAsync(List<MetricSnapshot> metricSnapshots)
        {
            if (metricSnapshots is null || metricSnapshots.Count < 1)
            {
                return;
            }
            
            var repoOwner = metricSnapshots.First().Owner;
            var repoName = metricSnapshots.First().Name;
            
            var parentDirectory = GetPathToRepoDataFiles(repoOwner, repoName);
            _fs.DirectoryCreateDirectory(parentDirectory);

            var saveTasks = metricSnapshots
                .GroupBy(s => s.Date.Date)
                // Have it throw, just in case the user hasn't aggregated their stats by date:
                .Select(dateGroup => dateGroup.Single())
                .Select(s =>
                {
                    var normalizedTimestamp = GetNormalizedTimestamp(s.Date.Date);
                    var fullPath = Path.Combine(parentDirectory, $"{normalizedTimestamp}{AnalysisSuffix}");
                    var json = JsonConvert.SerializeObject(s, _jsonSerializerSettings);
                    return (fullPath, json);
                })
                .Select(t => _fs.FileWriteAllTextAsync(t.fullPath, t.json));

            await Task.WhenAll(saveTasks);
        }

        public async ValueTask<MetricSnapshot> LoadAsync(string repoOwner, string repoName, DateTimeOffset timestamp)
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
            var fullPath = Path.Combine(parentDirectory, $"{normalizedTimestamp}{AnalysisSuffix}");
            var json = await _fs.FileReadAllTextAsync(fullPath);
            var snapshot = JsonConvert.DeserializeObject<MetricSnapshot>(json, _jsonSerializerSettings);
            return snapshot;
        }

        private string GetNormalizedTimestamp(DateTimeOffset timestamp)
        {
            var asUtc = timestamp.UtcDateTime;
            return $"{asUtc:yyyy-MM-dd}";
        }

        public async ValueTask<List<MetricSnapshot>> LoadHistoryAsync(string repoOwner, string repoName)
        {
            if (string.IsNullOrWhiteSpace(repoOwner))
            {
                throw new ArgumentNullException(nameof(repoOwner));
            }

            if (string.IsNullOrWhiteSpace(repoName))
            {
                throw new ArgumentNullException(nameof(repoName));
            }
            
            const string searchPattern = "*" + AnalysisSuffix;
            var searchPath = GetPathToRepoDataFiles(repoOwner, repoName);
            string[] matches;
            try
            {
                matches = _fs.DirectoryGetFiles(searchPath, searchPattern);
            }
            catch (DirectoryNotFoundException)
            {
                // DirectoryNotFound is normal when trying to read analytics from a WatchedRepo that was just added, and has never been analyzed or cached
                return null;
            }

            if (matches.Length < 1)
            {
                return new List<MetricSnapshot>();
            }

            var readMatches = matches
                .Select(async p => await _fs.FileReadAllTextAsync(p))
                .ToList();
            await Task.WhenAll(readMatches);

            var deserialized = readMatches
                .Select(t => t.Result)
                .Select(s => JsonConvert.DeserializeObject<MetricSnapshot>(s, _jsonSerializerSettings))
                .ToList();

            return deserialized;
        }
    }
}
