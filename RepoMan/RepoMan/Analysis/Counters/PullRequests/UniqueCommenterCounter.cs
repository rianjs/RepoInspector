using System.Linq;
using RepoMan.Repository;

namespace RepoMan.Analysis.Counters.PullRequests
{
    class UniqueCommenterCounter :
        IPullRequestCounter
    {
        public int Count(PullRequestDetails prDetails)
        {
            return prDetails.AllComments
                .Select(c => c.User.Id)
                .Distinct()
                .Count();
        }
    }
}