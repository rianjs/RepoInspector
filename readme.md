# Example usage
[As a daemon](https://github.com/rianjs/RepoMan/wiki/Daemon-example)

# Versioning
Uses [semver](https://semver.org):

> Given a version number MAJOR.MINOR.PATCH, increment the:
>
>   0) MAJOR version when you make incompatible API changes,
>   0) MINOR version when you add functionality in a backwards compatible manner, and
>   0) PATCH version when you make backwards compatible bug fixes.

# Publish instructions
To package up a new nuget version, and publish it, do the following:
1) Bump the version in `RepoInspector.csproj`
1) Update [release-notes.md](release-notes.md) with what has changed, referencing the issue, if applicable
1) In the directory containing `RepoInspector.csproj`, run `dotnet pack -c Release`
1) Upload to nuget
