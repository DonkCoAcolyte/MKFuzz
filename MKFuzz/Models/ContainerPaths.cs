namespace MKFuzz.Models;

public static class ContainerPaths
{
    // Mount points inside container
    public const string SourceFolder = "/workspace/src";
    public const string SourceMount = "/workspace/src_ro_mount";
    public const string SeedsMount = "/workspace/seeds";
    public const string OutputMount = "/workspace/hostout";

    // Build command templates
    public const string FuzzBuildPrefix = "cd " + SourceFolder + " &&  CC=afl-clang-fast CXX=afl-clang-fast++ ";
    public const string FuzzBuildSuffix = "";

    public const string CoverageBuildPrefix = "cd " + SourceFolder + " && CC=afl-clang-fast CXX=afl-clang-fast++ CFLAGS=\"--coverage -O0 -g\" CXXFLAGS=\"--coverage -O0 -g\" LDFLAGS=\"--coverage\" && ";
    public const string CoverageBuildSuffix = "";
}