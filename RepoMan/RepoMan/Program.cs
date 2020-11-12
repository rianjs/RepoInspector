using System;
using System.Collections.Generic;
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
            var owner = "alex";
            var repo = "nyt-2020-election-scraper";
            const int number = 368;
            var prReader = new GitHubRepoPullRequestReader(owner, repo, client);
            
            try
            {
                // https://github.com/alex/nyt-2020-election-scraper/pull/368 has comments, diff comments, and approvals with comments
                var allClosedPrs = await prReader.GetPullRequests(ItemStateFilter.Closed);
                var threeSixEight = allClosedPrs.Single(pr => pr.Number == number);

                var foo = await prReader.UpdateCommentsGraphAsync(threeSixEight);
                    
                // var diffReviewComments = await client.PullRequest.ReviewComment.GetAll(owner, repo, number);
                // // File.WriteAllText(CreateFullPath($"PullRequest-ReviewComment-GetAll-{number}.json"), Serialize(diffReviewComments));
                //
                //
                // var getAllForRepo = closedPrs[number];
                // File.WriteAllText(CreateFullPath($"PullRequest-GetAllForRepository-{number}.json"), Serialize(getAllForRepo));
                //
                // // State transitions (APPROVED), and comments associated with them
                // var approvalSummaries = await client.PullRequest.Review.GetAll(owner, repo, number);
                // // File.WriteAllText(CreateFullPath($"PullRequest-Review-GetAll-{number}.json"), Serialize(approvalSummaries));
                //
                // // These are the comments on the PR in general, not associated with an approval, or with a commit, or with something in the diff
                // var generalPrComments = await client.Issue.Comment.GetAllForIssue(owner, repo, number);
                // // File.WriteAllText(CreateFullPath($"Client-Issue-Comment-GetAllForIssue-{number}.json"), Serialize(generalPRComments));
                //
                // var pullRequestDetails = new PullRequestDetails(getAllForRepo)
                //     .WithDiffComments(diffReviewComments)
                //     .WithStateTransitionComments(approvalSummaries)
                //     .WithDiscussionComments(generalPrComments);
                //
                // var asUsefulPrs = closedPrs
                //     .Select(closed => new PullRequestDetails(closed));

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

        public static string Serialize(object o)
            => JsonConvert.SerializeObject(o, Formatting.Indented);

        public static string CreateFullPath(string fileName)
            => Path.Combine(_scratchDir, fileName);
    }
}