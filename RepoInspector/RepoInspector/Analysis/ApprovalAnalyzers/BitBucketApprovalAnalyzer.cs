using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using RepoInspector.Records;
using RepoInspector.Repository;

namespace RepoInspector.Analysis.ApprovalAnalyzers
{
    public class BitBucketApprovalAnalyzer
        : TextMatchingApprovalAnalyzer
    {
        public BitBucketApprovalAnalyzer(IOptionsSnapshot<PullRequestConstants> optionsSnapshot)
            : base(optionsSnapshot.Get(RepositoryKind.BitBucketCloud.ToString()), StringComparison.OrdinalIgnoreCase)
        {
        }
    }
}
