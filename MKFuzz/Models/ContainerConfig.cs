namespace MKFuzz.Models;

public static class ContainerConfig
{
    // Mount points inside container
    public const string SourceFolder = "/workspace/src";
    public const string SourceMount = "/workspace/src_ro_mount";
    public const string SeedsMount = "/workspace/seeds";
    public const string OutputMount = "/workspace/hostout";

    // Build command templates [old]
    //public const string FuzzBuildPrefix = "cd " + SourceFolder + " &&  CC=afl-clang-fast CXX=afl-clang-fast++ CFLAGS=\"-g -O0\" CXXFLAGS=\"-g -O0\" ";
    //public const string FuzzBuildSuffix = "";

    //public const string CoverageBuildPrefix = "cd " + SourceFolder + " && CC=clang-18 CXX=clang++-18 CFLAGS=\"-fprofile-instr-generate -fcoverage-mapping -O0 -g\" CXXFLAGS=\"-fprofile-instr-generate -fcoverage-mapping -O0 -g\" LDFLAGS=\"-fprofile-instr-generate -fcoverage-mapping\" ";
    //public const string CoverageBuildSuffix = "";

    // Default build commands for various builders and targets
    //cmake
    public const string cmakeFuzzBuildCommand = "cd " + SourceFolder + " && mkdir -p build_fuzz && cd build_fuzz && " +
    "cmake -DCMAKE_C_COMPILER=afl-clang-fast -DCMAKE_CXX_COMPILER=afl-clang-fast++ " +
    "-DCMAKE_C_FLAGS=\"-g -O0\" -DCMAKE_CXX_FLAGS=\"-g -O0\" " +
    ".. && " +
    "make -j4";
    public const string cmakeSanitizersBuildCommand = "cd " + SourceFolder + " && mkdir -p build_san && cd build_san && " +
    "AFL_USE_ASAN=1 AFL_USE_UBSAN=1 " +
    "cmake -DCMAKE_C_COMPILER=afl-clang-fast -DCMAKE_CXX_COMPILER=afl-clang-fast++ " +
    "-DCMAKE_C_FLAGS=\"-g -O0\" -DCMAKE_CXX_FLAGS=\"-g -O0\" " +
    ".. && " +
    "make -j4";
    public const string cmakeCmplogBuildCommand = "cd " + SourceFolder + " && mkdir -p build_cmplog && cd build_cmplog && " +
    "AFL_LLVM_CMPLOG=1 " +
    "cmake -DCMAKE_C_COMPILER=afl-clang-fast -DCMAKE_CXX_COMPILER=afl-clang-fast++ " +
    "-DCMAKE_C_FLAGS=\"-g -O0\" -DCMAKE_CXX_FLAGS=\"-g -O0\" " +
    ".. && " +
    "make -j4";
    public const string cmakeCovBuildCommand = "cd " + SourceFolder + " && mkdir -p build_cov && cd build_cov && " +
    "cmake -DCMAKE_C_COMPILER=clang-18 -DCMAKE_CXX_COMPILER=clang++-18 " +
    "-DCMAKE_C_FLAGS=\"-fprofile-instr-generate -fcoverage-mapping -O0 -g\" " +
    "-DCMAKE_CXX_FLAGS=\"-fprofile-instr-generate -fcoverage-mapping -O0 -g\" " +
    "-DCMAKE_EXE_LINKER_FLAGS=\"-fprofile-instr-generate -fcoverage-mapping\" " +
    ".. && " +
    "make -j4";

    //raw (this just demonstrates what flags are needed to the user for them to figure out whatever the hell they want to do)
    public const string rawFuzzBuildCommand = "cd " + SourceFolder + " &&  CC=afl-clang-fast CXX=afl-clang-fast++ CFLAGS=\"-g -O0\" CXXFLAGS=\"-g -O0\" ./configure --prefix=\"/workspace/fuzzbuild\" && make -j4 && make install && make clean";
    public const string rawSanitizersBuildCommand = "cd " + SourceFolder + " && AFL_USE_ASAN=1 AFL_USE_UBSAN=1 CC=afl-clang-fast CXX=afl-clang-fast++ CFLAGS=\"-g -O0\" CXXFLAGS=\"-g -O0\" ./configure --prefix=\"/workspace/sanbuild\" && make -j4 && make install && make clean";
    public const string rawCmplogBuildCommand = "cd " + SourceFolder + " && AFL_LLVM_CMPLOG=1 CC=afl-clang-fast CXX=afl-clang-fast++ CFLAGS=\"-g -O0\" CXXFLAGS=\"-g -O0\" ./configure --prefix=\"/workspace/cmplogbuild\" && make -j4 && make install && make clean";
    public const string rawCovBuildCommand = "cd " + SourceFolder + " && CC=clang-18 CXX=clang++-18 CFLAGS=\"-fprofile-instr-generate -fcoverage-mapping -O0 -g\" CXXFLAGS=\"-fprofile-instr-generate -fcoverage-mapping -O0 -g\" LDFLAGS=\"-fprofile-instr-generate -fcoverage-mapping\" ./configure --prefix=\"/workspace/covbuild\" && make -j4 && make install && make clean";
}