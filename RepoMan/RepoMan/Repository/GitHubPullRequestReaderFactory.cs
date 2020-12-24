using System;
using System.Collections.Generic;
using Octokit;
using RepoMan.Analysis.Normalization;
using RepoMan.Records;

namespace RepoMan.Repository
{
    public class GitHubPullRequestReaderFactory :
        IPullRequestReaderFactory
    {
        private readonly string _productHeaderValue;
        private readonly INormalizer _bodyNormalizer;
        private readonly Dictionary<string, GitHubClient> _clientsByApiKey;
        
        public GitHubPullRequestReaderFactory(string productHeaderValue, INormalizer bodyNormalizer)
        {
            if (string.IsNullOrWhiteSpace(productHeaderValue)) throw new ArgumentNullException(nameof(productHeaderValue));
            _productHeaderValue = productHeaderValue;
            _bodyNormalizer = bodyNormalizer ?? throw new ArgumentNullException(nameof(bodyNormalizer));
            _clientsByApiKey = new Dictionary<string, GitHubClient>(StringComparer.Ordinal);
        }

        private GitHubClient GetClient(WatchedRepository repo)
        {
            if (string.IsNullOrWhiteSpace(repo.ApiToken)) throw new ArgumentNullException(nameof(repo.ApiToken));
            if (string.IsNullOrWhiteSpace(repo.Url)) throw new ArgumentNullException(nameof(repo.Url));

            if (_clientsByApiKey.TryGetValue(repo.ApiToken, out var existingClient))
            {
                return existingClient;
            }

            var authority = new Uri(repo.Url).GetLeftPart(UriPartial.Authority);
            var github = new Uri(authority, UriKind.Absolute);
            var client = new GitHubClient(new ProductHeaderValue(_productHeaderValue), github);
            var auth = new Credentials(repo.ApiToken);
            client.Credentials = auth;

            _clientsByApiKey[repo.ApiToken] = client;
            return client;
        }

        public IRepoPullRequestReader GetReader(WatchedRepository repo)
        {
            if (repo is null) throw new ArgumentNullException(nameof(repo));
            var allowed = repo.RepositoryKind == RepositoryKind.GitHub || repo.RepositoryKind == RepositoryKind.GitHubEnterprise;
            if (!allowed)
            {
                throw new ArgumentException($"Watched repository must be of type {RepositoryKind.GitHub} or {RepositoryKind.GitHubEnterprise}, but was {repo.RepositoryKind}");
            }

            var client = GetClient(repo);
            return new GitHubRepoPullRequestReader(repo.Owner, repo.Name, client, _bodyNormalizer);
        }
    }
}