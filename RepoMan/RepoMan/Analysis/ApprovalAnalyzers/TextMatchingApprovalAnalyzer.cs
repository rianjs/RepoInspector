using System;
using System.Collections.Generic;
using System.Linq;
using RepoMan.Records;
using RepoMan.Repository;

namespace RepoMan.Analysis.ApprovalAnalyzers
{
    abstract class TextMatchingApprovalAnalyzer :
        IApprovalAnalyzer
    {
        private readonly StringComparison _comparison;
        private readonly HashSet<string> _approvalStates;
        private readonly HashSet<string> _nonApprovalStates;
        private readonly HashSet<string> _approvalTextFragments;

        protected TextMatchingApprovalAnalyzer(
            IEnumerable<string> approvalStateOptions,
            IEnumerable<string> noApprovalStateOptions,
            IEnumerable<string> approvalTextFragments,
            StringComparison comparison)
        {
            _comparison = comparison;
            _approvalStates = approvalStateOptions?.ToHashSet(StringComparer.FromComparison(_comparison))
                              ?? throw new ArgumentNullException(nameof(approvalStateOptions));
            _nonApprovalStates = noApprovalStateOptions?.ToHashSet(StringComparer.FromComparison(_comparison))
                                 ?? throw new ArgumentNullException(nameof(noApprovalStateOptions));
            _approvalTextFragments = approvalTextFragments?.ToHashSet(StringComparer.FromComparison(_comparison))
                                     ?? throw new ArgumentNullException(nameof(approvalTextFragments));
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
