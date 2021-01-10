using System;
using System.Linq;
using RepoInspector.Analysis.ApprovalAnalyzers;
using RepoInspector.Records;

namespace RepoInspector.Analysis.Scoring
{
    public class ApprovalScorer :
        PullRequestScorer
    {
        public const string Label = "ApprovalCount";
        public override string Attribute => Label;
        public override double ScoreMultiplier => 25;
        private readonly IApprovalAnalyzer _approvalAnalyzer;
        
        public ApprovalScorer(){}    // For deserialization only

        public ApprovalScorer(IApprovalAnalyzer approvalAnalyzer)
        {
            _approvalAnalyzer = approvalAnalyzer ?? throw new ArgumentNullException(nameof(approvalAnalyzer));
        }

        public override int Count(PullRequest prDetails)
        {
            return prDetails.Comments
                .Where(rc => _approvalAnalyzer.IsApproved(rc))
                .Select(rc => rc.User.Id)
                .Distinct()
                .Count();
        }
    }
}