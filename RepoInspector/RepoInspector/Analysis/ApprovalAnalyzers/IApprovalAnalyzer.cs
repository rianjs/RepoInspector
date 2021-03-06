using RepoInspector.Records;

namespace RepoInspector.Analysis.ApprovalAnalyzers
{
    public interface IApprovalAnalyzer
    {
        /// <summary>
        /// Returns the number of unique approvals using a variety of analysis techniques.
        /// </summary>
        /// <param name="comment"></param>
        /// <returns></returns>
        public bool IsApproved(Comment comment);
    }
}
