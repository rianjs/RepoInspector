using System.Collections.Generic;

namespace RepoInspector.Records
{
    public class PullRequestConstants
    {
        public IReadOnlyList<string> ExplicitApprovals { get; set; }
        public IReadOnlyList<string> ExplicitNonApprovals { get; set; }
        public IReadOnlyList<string> ImplicitApprovals { get; set; }
    }
}
