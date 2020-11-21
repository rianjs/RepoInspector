using System;
using System.Collections.Generic;
using System.Linq;

namespace RepoMan.PullRequest
{
    public class ApprovalAnalyzer :
        IApprovalAnalyzer
    {
        private static readonly StringComparison _comparison = StringComparison.OrdinalIgnoreCase;
        private readonly HashSet<string> _approvalStates;
        private readonly HashSet<string> _nonApprovalStates;
        private readonly HashSet<string> _approvalTextFragments;

        public ApprovalAnalyzer(
            IEnumerable<string> approvalStateOptions,
            IEnumerable<string> noApprovalStateOptions,
            IEnumerable<string> approvalTextFragments)
        {
            _approvalStates = approvalStateOptions.ToHashSet(StringComparer.FromComparison(_comparison));
            _nonApprovalStates = noApprovalStateOptions.ToHashSet(StringComparer.FromComparison(_comparison));
            _approvalTextFragments = approvalTextFragments.ToHashSet(StringComparer.FromComparison(_comparison));
        }

        /// <summary>
        /// <inheritdoc cref="IApprovalAnalyzer.IsApproved"/>
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public bool IsApproved(Comment comment)
        {
            if (_approvalStates.Contains(comment.ReviewState))
            {
                return true;
            }

            if (_nonApprovalStates.Contains(comment.ReviewState))
            {
                return false;
            }
            
            // Check for implicit approvals
            if (string.IsNullOrWhiteSpace(comment.Text))
            {
                return false;
            }

            return _approvalTextFragments.Any(possibility => comment.Text.Equals(possibility, _comparison));
        }
    }
}