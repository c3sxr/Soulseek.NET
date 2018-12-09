#addin nuget:?package=Cake.Git
public class Settings
{
    private readonly ICakeContext context;

    #region Directories
    public DirectoryPath BaseOutputDirectory { get; }
    public DirectoryPath TestResultsDirectory => BaseOutputDirectory.Combine("TestResults");
    public DirectoryPath CoverageResultsDirectory => BaseOutputDirectory.Combine("Coverage");
    public DirectoryPath CoverageReportDirectory => BaseOutputDirectory.Combine("CoverageReport");
    public DirectoryPath PackageOutputDirectory => BaseOutputDirectory.Combine("Packages");

    public IEnumerable<DirectoryPath> AllDirectories()
    {
        yield return BaseOutputDirectory;
        yield return TestResultsDirectory;
        yield return CoverageResultsDirectory;
        yield return CoverageReportDirectory;
        yield return PackageOutputDirectory;
    }
    #endregion
    public bool ForcePublish { get; set; }
    public bool ShouldPublish => !BuildSystem.IsLocalBuild && !IsPullRequest(CurrentBranch);

    public Settings(ICakeContext context, DirectoryPath baseOutputDirectory)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        BaseOutputDirectory = baseOutputDirectory ?? throw new ArgumentNullException(nameof(baseOutputDirectory));
    }

    BuildSystem BuildSystem => context.BuildSystem();
    GitBranch CurrentBranch => context.GitBranchCurrent("./");

    bool IsPullRequest(GitBranch branch)
    {
        if (string.IsNullOrEmpty(context.EnvironmentVariable("Build.BuildID")))
            return context.EnvironmentVariable("Build.SourceBranch").Contains("refs/pull/");
        return branch.RemoteName.Contains("refs/pull/");
    }
}