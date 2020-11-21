using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Octokit;
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

            var cachePath = Path.Combine(_scratchDir, $"{owner}-{repo}-prs.json");
            var fs = new Filesystem.Filesystem();
            var wordCounter = new WordCounter();
            
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

                var prCount = 0;
                var approvals = 0;
                var wordCountsByPr = new Dictionary<int, int>();
                var commentCountByPr = new Dictionary<int, int>();
                await foreach (var pr in repoHistoryMgr.GetPullRequestsAsync())
                {
                    prCount++;
                    var wordCount = pr.AllComments.Select(c => wordCounter.CountWords(c.Text)).Sum();
                    wordCountsByPr[pr.Number] = wordCount;
                    var commentCount = pr.AllComments.Count();
                    commentCountByPr[pr.Number] = commentCount;
                }

                // PR statistics of interest:
                // Approval count per PR
                // Comment count per PR
                // Most comments (pr => {})
                
                // COMMENTARY CHARACTERISTICS
                // How robust is the dialog when it needs to be?
                // HYPOTHESIS: strong teams have PRs with either a lot or very little dialog, depending on the scope of changes proposed. Small changesets are
                // easy to approve and often have no comments. Design changes will often engender a LOT of comments. So in a health repo, you'd expect to see a 
                // small number of comments consistently (rarely zero), and a huge number of comments once in a while, often in a PR that stays open longer than
                // the median
                // MEASURES:
                // Median word count per comment (should always be > 1, otherwise it should just be an approval)
                
                // Comment complexity per PR (i.e. how robust was the dialog?)
                // Median word count per comment
                // Most 

                // APPROVAL/MERGE CHARACTERISTICS
                // PRs shouldn't get stale. They should have a turnaround time of 1-2 business days in most cases. PRs open longer than that should have a lot
                // dialog.
                
                
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