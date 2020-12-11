using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using RepoMan;
using RepoMan.Records;
using RepoMan.Repository;
using Serilog;

namespace Scratch
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var logger = GetLogger();
            var clock = new Clock();
            var scratchDir = Path.Combine("/", "Users/rianjs/dev/RepoMan/scratch");
            var configPath = Path.Combine(scratchDir, "repoman-config.json");
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile(configPath, optional: false, reloadOnChange: true)
                .Build();

            var serviceCollection = new ServiceCollection()
                .Configure<PullRequestConstants>(RepositoryKind.GitHub.ToString(),
                    configuration.GetSection("PRConstants:GitHub"))
                .Configure<PullRequestConstants>(RepositoryKind.BitBucketCloud.ToString(),
                    configuration.GetSection("PRConstants:BitBucketCloud"))
                .Configure<List<WatchedRepository>>("WatchedRepositories", configuration.GetSection("WatchedRepositories"));

            var sp = serviceCollection.BuildServiceProvider();
            var watchedRepos = configuration.GetSection("WatchedRepositories").Get<List<WatchedRepository>>();
            
            var jsonSettings = GetDebugJsonSerializerSettings();
            var compressingRefreshingDnsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromSeconds(120),
                AutomaticDecompression = DecompressionMethods.All,
            };
            // var hostname = "https://bitbucket.org";
            // var repoOwner = "medicalinformatics";
            // var repoName = "mainzelliste";
            
            var client = new HttpClient(compressingRefreshingDnsHandler);
            // var bbReader = new BitBucketCloudPullRequestReader(hostname, repoOwner, repoName, client, jsonSettings, clock, logger);
            // var result = await bbReader.GetPullRequestsRootAsync(ItemState.Closed);

            // https://bitbucket.org/medicalinformatics/mainzelliste/pull-requests/91/refactor-clean-up-code-in-create-patient/diff -- 22 activities!
            // var baz = await bbReader.TryFillCommentGraphAsync(new PullRequest {Number = 91});
            
            // var watchedRepos = new List<WatchedRepository>
            // {
            //     new WatchedRepository
            //     {
            //         Owner = "alex",
            //         Name = "nyt-2020-election-scraper",
            //         Description = "NYT election data scraper and renderer",
            //         ApiToken = "f13d7a9bc50cce86b01a4389baf144317d221ea0",
            //         Url = "https://github.com",
            //         RepositoryKind = RepositoryKind.GitHub,
            //     },
            //     new WatchedRepository
            //     {
            //         Owner = "rianjs",
            //         Name = "ical.net",
            //         Description = "RFC-5545 ical data library",
            //         ApiToken = "f13d7a9bc50cce86b01a4389baf144317d221ea0",
            //         Url = "https://github.com",
            //         RepositoryKind = RepositoryKind.GitHub,
            //     },
            //     new WatchedRepository
            //     {
            //         Owner = "medicalinformatics",
            //         Name = "mainzelliste",
            //         Description = "Mainzelliste is a web-based first-level pseudonymization service",
            //         ApiToken = null,
            //         Url = "https://bitbucket.org",
            //         RepositoryKind = RepositoryKind.BitBucketCloud,
            //     },
            // };
            //
            // var serialized = JsonConvert.SerializeObject(watchedRepos, jsonSettings);
            // var deserialized = JsonConvert.DeserializeObject<List<WatchedRepository>>(serialized);
            
            Console.WriteLine("Hello World!");
        }

        public static string GetJson()
        {
            var path = @"/Users/rianjs/dev/RepoMan/scratch/scratch.json";
                
            return File.ReadAllText(path);
        }
        
        private static ILogger GetLogger()
        {
            return new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();
        }

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