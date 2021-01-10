using System.Collections.Generic;

namespace RepoMan.Records
{
    public class PullRequestConstants
    {
        public IReadOnlyList<string> ExplicitApprovals { get; set; }
        public IReadOnlyList<string> ExplicitNonApprovals { get; set; }
        public IReadOnlyList<string> ImplicitApprovals { get; set; }
    }
}
