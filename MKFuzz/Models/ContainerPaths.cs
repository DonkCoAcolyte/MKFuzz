namespace MKFuzz.Models;

public static class ContainerPaths
{
    // Mount points inside container
    public const string SourceMount = "/workspace/src";
    public const string SeedsMount = "/workspace/seeds";
    public const string OutputMount = "/workspace/hostout";

    // Build command templates
    public const string FuzzBuildPrefix = "cd " + SourceMount + " && ";
    public const string FuzzBuildSuffix = " CC=afl-clang-fast CXX=afl-clang-fast++";

    public const string CoverageBuildPrefix = "cd " + SourceMount + " && ";
    public const string CoverageBuildSuffix = " CFLAGS=\"--coverage -O0 -g\" CXXFLAGS=\"--coverage -O0 -g\" LDFLAGS=\"--coverage\"";
}