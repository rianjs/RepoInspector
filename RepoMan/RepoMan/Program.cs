using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Octokit;

namespace RepoMan
{
    class Program
    {
        private static readonly string _tokenPath = Path.Combine(GetScratchDirectory(), "repoman-pan.secret");
        private static readonly string _scratchDir = GetScratchDirectory();
        private static readonly string _token = File.ReadAllText(_tokenPath).Trim();

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
            
            
            Console.WriteLine("Hello World!");
        }

        private static async Task Debug(GitHubClient client)
        {
            try
            {
                var owner = "alex";
                var repo = "nyt-2020-election-scraper";
                // var pr = await client.PullRequest.Get("dotnet", "roslyn", 14645);    // returns the proper comment count
                // var user = client.Repository.GetAllForUser("bojanrajkovic");
                // var org = await client.Repository.GetAllForOrg("Cimpress");
                // var alexPrs = await client.Repository.Get("alex", "nyt-2020-election-scraper");
                var prOpts = new PullRequestRequest
                {
                    State = ItemStateFilter.Closed,
                    // We should be able to do a binary search on the resultant list for the PR that has a last comment 
                    SortProperty = PullRequestSort.Popularity,
                };
                var closedPrs = await client.PullRequest.GetAllForRepository(owner, repo, prOpts);
                // var derp = await client.PullRequest.Review.GetAllComments(owner, repo, closedPrs.First().Number)
                var foo = closedPrs.First();

                var deepPrQuery = closedPrs
                    .Take(5)
                    .Select(pr => client.Issue.Comment.GetAllForIssue(owner, repo, pr.Number))
                    .ToList();
                
                await Task.WhenAll(deepPrQuery);

                var success = deepPrQuery
                    .Where(t => t.IsCompletedSuccessfully)
                    .Select(t => t.Result)
                    .ToList();
                
                var fail = deepPrQuery
                    .Where(t => t.IsFaulted)
                    .ToList();
                
                // PR statistics of interest:
                // Approval count per PR
                // Comment count per PR
                // Comment complexity per PR (i.e. how robust was the dialog?)
                // Comments per line changed
                
                // Other
                // Commits merged directly to master
                
                
                var shallowResult = Path.Combine(_scratchDir, "shallow-prs.json");
                var shallowWriteTask = File.WriteAllTextAsync(shallowResult, JsonConvert.SerializeObject(closedPrs, Formatting.Indented));
                var successResult = Path.Combine(_scratchDir, "deep-prs.json");
                var deepWriteTask = File.WriteAllTextAsync(successResult, JsonConvert.SerializeObject(success, Formatting.Indented));
                await Task.WhenAll(shallowWriteTask, deepWriteTask);
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
            //var repoRoot = Enumerable.Range(0, 5)
            //    .Select(path => Directory.GetParent(path))
            //    .LastOrDefault() ?? throw new ArgumentNullException();


            //var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            //var dev = Path.Combine(userDir, "dev");
            //var thisRepo = Path.Combine(dev, "RepoMan");
            //var scratch = Path.Combine(thisRepo, "scratch");
            //return scratch;
        }
    }
}