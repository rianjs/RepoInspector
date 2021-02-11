using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RepoInspector.Records
{
    public class PullRequestConstants
    {
        [Required]
        public IReadOnlyList<string> ExplicitApprovals { get; set; }

        [Required]
        public IReadOnlyList<string> ExplicitNonApprovals { get; set; }

        [Required]
        public IReadOnlyList<string> ImplicitApprovals { get; set; }
    }
}
