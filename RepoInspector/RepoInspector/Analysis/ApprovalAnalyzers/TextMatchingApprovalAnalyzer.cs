using System;
using System.Collections.Generic;
using System.Linq;
using RepoInspector.Records;
using RepoInspector.Repository;

namespace RepoInspector.Analysis.ApprovalAnalyzers
{
    public abstract class TextMatchingApprovalAnalyzer :
        IApprovalAnalyzer
    {
        private readonly StringComparison _comparison;
        private readonly HashSet<string> _approvalStates;
        private readonly HashSet<string> _nonApprovalStates;
        private readonly HashSet<string> _approvalTextFragments;

        protected TextMatchingApprovalAnalyzer(PullRequestConstants prConstants, StringComparison comparison)
        {
            _comparison = comparison;
            _approvalStates = prConstants.ExplicitApprovals?.ToHashSet(StringComparer.FromComparison(_comparison))
                              ?? throw new ArgumentNullException(nameof(prConstants.ExplicitApprovals));
            _nonApprovalStates = prConstants.ExplicitNonApprovals?.ToHashSet(StringComparer.FromComparison(_comparison))
                                 ?? throw new ArgumentNullException(nameof(prConstants.ExplicitNonApprovals));
            _approvalTextFragments = prConstants.ImplicitApprovals?.ToHashSet(StringComparer.FromComparison(_comparison))
                                     ?? throw new ArgumentNullException(nameof(prConstants.ImplicitApprovals));
        }

        /// <summary>
        /// <inheritdoc cref="IApprovalAnalyzer.IsApproved"/>
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public bool IsApproved(Comment comment)
        {
            if (comment.ReviewState is null)
            {
                return false;
            }
            
            // TODO: This is so ugly...
            var asString = comment.ReviewState?.ToOctokitString();
            if (_approvalStates.Contains(asString))
            {
                return true;
            }

            if (_nonApprovalStates.Contains(asString))
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
