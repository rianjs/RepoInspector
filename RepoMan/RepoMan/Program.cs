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
using RepoMan.RepoHistory;
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

            var cachePath = Path.Combine(_scratchDir, "pull-requests.json");
            var fs = new Filesystem.Filesystem();
            
            try
            {
                var repoHistoryMgr = await RepoHistoryManager.InitializeAsync(fs, cachePath, prReader, _jsonSerializerSettings, _logger);
                var populationDelay = TimeSpan.FromSeconds(0.5);
                
                // Initialize the cache
                // if it's empty, try to fill it up, or create 
                
                
                await repoHistoryMgr.PopulateUnfinishedPullRequestsAsync(populationDelay);

                var isEmpty = (await repoHistoryMgr.GetPullRequestCount()) == 0;
                if (isEmpty)
                {
                    var incompleteClosedPrs = await prReader.GetPullRequests(ItemStateFilter.Closed);
                    await repoHistoryMgr.ImportPullRequestsAsync(incompleteClosedPrs);
                    // await repoHistoryMgr.
                    // var foo = await repoHistoryManager.ImportBatchAsync(closedPrs);
                }

                // Read the cache
                // Find the elements that haven't been fully populated
                // Populate them until we can't anymore
                // Write down the result
                
                // https://github.com/alex/nyt-2020-election-scraper/pull/368 has comments, diff comments, and approvals with comments
                var allClosedPrs = await prReader.GetPullRequests(ItemStateFilter.Closed);

                foreach (var pr in allClosedPrs)
                {
                    var succeeded = await prReader.TryFillCommentGraphAsync(pr);
                    if (!succeeded)
                    {
                        break;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                
                // PR statistics of interest:
                // Approval count per PR
                // Comment count per PR
                // Comment complexity per PR (i.e. how robust was the dialog?)
                // Comments per line changed

                // Other
                // Commits merged directly to master

                // var shallowResult = Path.Combine(_scratchDir, "shallow-prs.json");
                // var shallowWriteTask = File.WriteAllTextAsync(shallowResult, JsonConvert.SerializeObject(closedPrs, Formatting.Indented));
                // var successResult = Path.Combine(_scratchDir, "deep-prs.json");
                // var deepWriteTask = File.WriteAllTextAsync(successResult, JsonConvert.SerializeObject(success, Formatting.Indented));
                // await Task.WhenAll(shallowWriteTask, deepWriteTask);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                // throw;
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