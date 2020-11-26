using RepoMan.Repository;

namespace RepoMan.Analysis.Counters.PullRequests
{
    interface IPullRequestCounter
    {
        int Count(PullRequestDetails prDetails); 
    }
}