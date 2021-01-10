using RepoInspector.Records;

namespace RepoInspector.Repository
{
    public interface IPullRequestReaderFactory
    {
        IRepoPullRequestReader GetReader(WatchedRepository repo);
    }
}