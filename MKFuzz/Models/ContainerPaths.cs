namespace MKFuzz.Models;

public static class ContainerPaths
{
    // Mount points inside container
    public const string SourceFolder = "/workspace/src";
    public const string SourceMount = "/workspace/src_ro_mount";
    public const string SeedsMount = "/workspace/seeds";
    public const string OutputMount = "/workspace/hostout";

    // Build command templates
    public const string FuzzBuildPrefix = "cd " + SourceFolder + " &&  CC=afl-clang-fast CXX=afl-clang-fast++ CFLAGS=\"-g -O0\" CXXFLAGS=\"-g -O0\" ";
    public const string FuzzBuildSuffix = "";

    public const string CoverageBuildPrefix = "cd " + SourceFolder + " && CC=clang-18 CXX=clang++-18 CFLAGS=\"-fprofile-instr-generate -fcoverage-mapping -O0 -g\" CXXFLAGS=\"-fprofile-instr-generate -fcoverage-mapping -O0 -g\" LDFLAGS=\"-fprofile-instr-generate -fcoverage-mapping\" ";
    public const string CoverageBuildSuffix = "";
}