using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using RepoInspector.Records;
using RepoInspector.Repository;

namespace RepoInspector.Analysis.ApprovalAnalyzers
{
    public class GitHubApprovalAnalyzer
        : TextMatchingApprovalAnalyzer
    {
        public GitHubApprovalAnalyzer(IOptionsSnapshot<PullRequestConstants> prConstantOptions)
            : base(prConstantOptions.Get(RepositoryKind.GitHub.ToString()), StringComparison.OrdinalIgnoreCase)
        {}
    }
}
