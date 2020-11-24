using System.Collections.Generic;

namespace RepoMan.Repository
{
    class PullRequestConstants
    {
        public IReadOnlyList<string> ExplicitApprovals { get; set; }
        public IReadOnlyList<string> ExplicitNonApprovals { get; set; }
        public IReadOnlyList<string> ImplicitApprovals { get; set; }
    }
}
