using System;
using System.Collections.Generic;

namespace RepoMan.Analysis.ApprovalAnalyzers
{
    public class GitHubApprovalAnalyzer
        : TextMatchingApprovalAnalyzer
    {
        public GitHubApprovalAnalyzer(
            IEnumerable<string> approvalStateOptions,
            IEnumerable<string> noApprovalStateOptions,
            IEnumerable<string> approvalTextFragments)
            : base(approvalStateOptions, noApprovalStateOptions, approvalTextFragments, StringComparison.OrdinalIgnoreCase)
        {}
    }
}
