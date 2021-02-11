using System;
using Microsoft.Extensions.Logging;
using RepoInspector.Records;

namespace RepoInspector.Repository
{
    public class PullRequestReaderFactory :
        IPullRequestReaderFactory
    {
        private readonly GitHubPullRequestReaderFactory _ghReaderFactory;
        private readonly BitBucketCloudPullRequestReaderFactory _bbReaderFactory;
        private readonly ILogger _logger;

        public PullRequestReaderFactory(GitHubPullRequestReaderFactory ghClientFactory, BitBucketCloudPullRequestReaderFactory bbCloudClientFactory, ILogger<PullRequestReaderFactory> logger)
        {
            _ghReaderFactory = ghClientFactory ?? throw new ArgumentNullException(nameof(ghClientFactory));
            _bbReaderFactory = bbCloudClientFactory ?? throw new ArgumentNullException(nameof(bbCloudClientFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IRepoPullRequestReader GetReader(WatchedRepository repo)
        {
            switch (repo.RepositoryKind)
            {
                case RepositoryKind.GitHub:
                case RepositoryKind.GitHubEnterprise:
                    return _ghReaderFactory.GetReader(repo);
                case RepositoryKind.BitBucketCloud:
                    return _bbReaderFactory.GetReader(repo);
                case RepositoryKind.BitBucketServer:
                    throw new NotImplementedException($"{RepositoryKind.BitBucketServer} is not implemented yet");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
