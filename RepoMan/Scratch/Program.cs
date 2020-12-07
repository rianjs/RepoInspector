using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using RepoMan;
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
            var jsonSettings = GetDebugJsonSerializerSettings();
            var compressingRefreshingDnsHandler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromSeconds(120),
                AutomaticDecompression = DecompressionMethods.All,
            };

            var hostname = "https://bitbucket.org";
            var repoOwner = "medicalinformatics";
            var repoName = "mainzelliste";
            
            var client = new HttpClient(compressingRefreshingDnsHandler);
            var bbReader = new BitBucketCloudPullRequestReader(hostname, repoOwner, repoName, client, jsonSettings, clock, logger);
            var result = await bbReader.GetPullRequestsRootAsync(ItemState.Closed);
            
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