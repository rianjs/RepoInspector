using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Octokit;
using RepoMan.Analysis;
using RepoMan.PullRequest;
using RepoMan.Repository;
using Serilog;

namespace RepoMan
{
    class Program
    {
        private static readonly string _tokenPath = Path.Combine(GetScratchDirectory(), "repoman-pan.secret");
        private static readonly string _scratchDir = GetScratchDirectory();
        private static readonly string _token = File.ReadAllText(_tokenPath).Trim();
        private static readonly JsonSerializerSettings _jsonSerializerSettings = GetDebugJsonSerializerSettings();
        private static readonly ILogger _logger = GetLogger();

        static async Task Main(string[] args)
        {
            await Task.Delay(0);
            var url = args.Length > 1
                ? args[1]
                : "https://github.com/microsoft";

            var apiToken = args.Length > 2
                ? args[2]
                : _token;

            var client = GetClient(url, apiToken);
            await Debug(client);
        }

        private static async Task Debug(GitHubClient client)
        {
            var owner = "alex";
            var repo = "nyt-2020-election-scraper";
            const int number = 368;
            var prReader = new GitHubRepoPullRequestReader(owner, repo, client);
            var dosBuffer = TimeSpan.FromSeconds(0.5);
            var repoHealthAnalyzer = new RepositoryHealthAnalyzer();

            var approvalStates = new[] {"APPROVED"};
            var nonApprovalStates = new[] {"CHANGES_REQUESTED", "COMMENTED", "DISMISSED", "PENDING"};
            var implicitApprovals = new[]
            {
                "lgtm",
                "ok to merge",
                "go ahead and merge",
                "looks good to me",
            };
            

            var cachePath = Path.Combine(_scratchDir, $"{owner}-{repo}-prs.json");
            var fs = new Filesystem.Filesystem();
            var wordCounter = new WordCounter();
            var approvalAnalyzer = new ApprovalAnalyzer(approvalStates, nonApprovalStates, implicitApprovals);
            var commentAnalyzer = new CommentAnalyzer(approvalAnalyzer, wordCounter);
            
            try
            {
                IRepoHistoryManager repoHistoryMgr = await RepoHistoryManager.InitializeAsync(
                    fs: fs,
                    cachePath: cachePath,
                    prReader: prReader,
                    prApiDosBuffer: dosBuffer,
                    refreshFromUpstream: true,
                    jsonSerializerSettings: _jsonSerializerSettings,
                    logger: _logger);

                var pullRequestSnapshots = await repoHistoryMgr.GetPullRequestsAsync();
                
                _logger.Information($"Starting deep evaluation of {pullRequestSnapshots.Count} pull requests");
                var singularCommentComputeTimer = Stopwatch.StartNew();
                var singularPrSnapshots = pullRequestSnapshots
                    .Select(pr => commentAnalyzer.CalculateCommentStatistics(pr))
                    .ToDictionary(pr => pr.Number, pr => pr);
                singularCommentComputeTimer.Stop();
                _logger.Information($"Deep evaluation of {pullRequestSnapshots.Count} pull requests completed in {singularCommentComputeTimer.Elapsed.ToMicroseconds():N0} microseconds");

                _logger.Information($"Calculating repository health statistics for {owner}:{repo} repository which has {pullRequestSnapshots.Count:N0} pull requests");
                var repoHealthTimer = Stopwatch.StartNew();
                var repoHealth = repoHealthAnalyzer.CalculateRepositoryHealthStatistics(singularPrSnapshots.Values);
                repoHealthTimer.Stop();
                _logger.Information($"Repository health statistics for {owner}:{repo} calculated in {repoHealthTimer.Elapsed.ToMicroseconds():N0} microseconds");
                _logger.Information($"{Environment.NewLine}{JsonConvert.SerializeObject(repoHealth, _jsonSerializerSettings)}");
                
                // Other
                // Commits merged directly to master
                
                Console.WriteLine("Done");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static GitHubClient GetClient(string repoUrl, string token)
        {
            var github = new Uri(repoUrl);
            var client = new GitHubClient(new ProductHeaderValue("repoman-health-metrics"), github);
            var auth = new Credentials(token);
            client.Credentials = auth;
            return client;
        }

        private static string GetScratchDirectory()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            const int netCoreNestingLevel = 5;

            for (var i = 0; i < netCoreNestingLevel; i++)
            {
                path = Directory.GetParent(path).FullName;
            }

            return Path.Combine(path, "scratch");
        }
        
        private static ILogger GetLogger() =>
            new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

        public static string CreateFullPath(string fileName)
            => Path.Combine(_scratchDir, fileName);
        
        private static JsonSerializerSettings GetDebugJsonSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                //For demo purposes:
                DefaultValueHandling = DefaultValueHandling.Include,
                Formatting = Formatting.Indented,
                //Otherwise:
                // DefaultValueHandling = DefaultValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                Converters = new List<JsonConverter> { new StringEnumConverter(), },
            };
        }
    }
}