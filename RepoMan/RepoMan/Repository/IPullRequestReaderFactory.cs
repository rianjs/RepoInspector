using RepoMan.Records;

namespace RepoMan.Repository
{
    public interface IPullRequestReaderFactory
    {
        IRepoPullRequestReader GetReader(WatchedRepository repo);
    }
}