A gui attached to a docker container with a set up c++ fuzzing pipeline. I believe in industry terms this is a fuzzing orchestrator.
I should note that the dockerfile contains not only the necessary c++ fuzzing tools (afl++, afl-utils, afl-cov-fast and dependancies), but also comes with c# tools preinstalled because during the initial brainstorm it was planned to implement such functionality aswell. Its also unoptimized as hell containing a bunch of unnecessary installs that are only there because i was too lazy to check which dependancies were necessary for what functionality of afl-fuzz or others.
# Program overview
*For the program to work you obviously need to have docker installed, aswell as you need to have pre-downloaded/installed/built two docker images: alpine:latest (available online) and the custom image coming from the dockerfile in assets. The custom dockerfile image is to be named fuzzing-image:latest. That should be good enough to go.

*Immediately upon launch close to the top you will see the three tabs available: Project Setup, Fuzzing and Results. While they are self-explanatory, we will go further in depth below.

*As it is right now, the program is only good for fuzzing c++ apps that accept their input through a file.
## <b>Project setup</b>
The project setup tab is supposed to be where you select all relevant inputs and options prior to fuzzing. Note the fact that configurations can be saved/opened using the file menu at the top. ALL THE COPIED TO:/MOUNTED TO: PATHS REFER TO PATHS INSIDE THE FUZZING DOCKER CONTAINER, NOT THE HOST SYSTEM.
<img src="MKFuzz/Assets/showcase images/projectSetup.png"/>
Lets go over the elements one by one:
- the text field for the project name, "New Project" by default, is currently useless/cosmetic. It will save and load, but it does not influence anything but itself.
- the Source folder field allows you to input the path to your source code folder, hopefully containing a makefile (i know there should probably be more robust options for different build implements but currently only make flags are accounted for). The contents of this folder will be copied inside the container over to the /workspace/src folder. (Due to the way development went, its actually mounted at /workspace/src_ro_mount as a volume aswell, but that isnt used for anything but the copy operation).
- the Harness File field is a decorative field as it is right now. Pressing the Edit option will "execute" whatever is currently selected, thus launching the associated editor for the files of that extention (maybe). I thought to myself that it would be nice to be able to select a file/function and have the program itself generate a harness and build requisites on its own, but i dont think its something that will come quick and easy. Its going to have to be something that MAYBE gets made somewhere in the future. Cant be bothered right now.
- the Seeds folder field selects the folder the files from which will be taken by afl-fuzz to be fed to your target binary. There should be ATLEAST one file there, as im pretty sure afl-fuzz doesnt quite work otherwise with my setup.
- the Output folder is the folder where the resulting files will be dumped. It is mounted as a volume to the fuzzing container, which proceeds to write files there, so make sure you are not using that folder for anything else.

- the Docker status display tells you if your Docker setup is good to go. The check environment button (the corresponding command for which is also launched at app startup) launches an alpine:latest container to merely check /proc/sys/kernel/core_pattern. If its not yet set, it re-launches an alpine container, now PRIVILEGED, which sets it to "core". Notifies you on success/fail. Both containers are deleted at the end of their operation.
### Build and target dropdown menu
this is where you are supposed to specify the commands used to build the fuzzing/coverage binaries, aswell as to specify where they will end up and what arguments will be fed to them on launch.
- the Fuzz build command field is exactly what it is: the command that will be run in order to build the fuzzing binary. What ends up being executed inside the container is prefix + input field contents + postfix. Make sure to specify different output names/directories for the two build versions!
- the Coverage build command field is the same. The flags are a bit different, but the idea is the same. Make sure to specify different output names/directories for the two build versions!
- the Fuzz binary path (in container) field determines what executable will be launched during afl-fuzz (container#FuzzBinaryPath TargetArguments). The input arguments are not to be included here. Importantly, the path starts at the root of the container file system, so your path should be something close to /workspace/src/output.exe
- the Coverage binary path is more of the same.
- The target arguments field string will be used in place of the arguments for the launched binaries in both cases. As is standart afl script, the fuzzer generated file input is marked as @@. Its recommended to use /dev/null as output since it is presumed that we dont really care about it if the program does return something, and we dont want the various fuzzing processes to lock up trying to access/write/read one file.

### Fuzzing settings
What i dont think belongs above, i put here. Contains various options.

- the CPU cores count, determines the amount of cores used for the fuzzing process. Contains all your cores by default, so be careful.
- the stop condition selector. Currently only has two options: Manual (you will press the button to stop), CrashCount (will automatically "press" the stop button once the fuzzing session discovers > Crashes crashes.
- the Crashes field sets the value of crashes at which the CrashCount stop condition stops fuzzing.
- the Generate coverage report checkbox decides if coverage related operations will even be attempted. If you unselect this, coverage build wont be attempted and no LCOV coverage report will be generated during post-processing.
- the Keep container (alive) checkbox makes sure your container doesnt get killed on app exit. If this option is unset your fuzzing container will be automatically deleted on proper exit.
- the Sanitize filenames (crossrenames) checkbox applies the filename sanitizer to all afl-fuzz minimized queue and crash items prior to being copied over to the host system. These files generate with names banned on windows, so this should be kept on to avoid problems.

### Buttons in the corner (SE)
- the Save Project button saves the current project set up in a .json file. Later that file can be opened using the option in the file menu up top.
- the Start Container button starts the fuzzing-image:latest container. The button starts disabled, only activated once ANYTHING is input into the Source Folder field. This isnt really a smart way to go about it, will revise it in the future. Not right now thoughhhhhhh.

## Fuzzing
The ability to select this tab (or the results tab for that matter) only unlocks if you start the fuzzing container. Once you delete the container, you will once again lose access to this tab (done because all buttons here make calls to the container that you start).
<img src="MKFuzz/Assets/showcase images/fuzzing.png"/>
- the Build Binaries button launches both the fuzz build command and the coverage build command (if coverage is enabled) in sequence. The process will produce messages in the log console (the left one).
- the Start Fuzzing button starts the fuzzing process using afl-multicore and an auto-generated config.
  var config = new
{
    target = project.FuzzBinaryPath,
    cmdline = project.TargetArgs,
    input = "/workspace/seeds",
    output = "/workspace/sync",
    memory = project.MemoryLimit.ToString(), //these two are hardcoded in, not exposed in project setup
    timeout = project.TimeoutMs.ToString() //i should probably change that, but it didnt quite come up yet.
};

- the Resume Fuzzing button resumes the stopped fuzzing session.
- the Stop Fuzzing button stops fuzzing using afl-multikill. Pressing it multiple times does nothing.
- the Analyze results button does many things: collects crashes, minimizes the corpus (queue), generates a coverage report based on the minimized corpus (because it also makes sure no crashes are present and it shouldnt lose us coverage, in theory), saves the coverage report, sanitizes the corpus/crashes filenames, transfers them over to the output directory volume mount. After all that, processing is complete.

You may notice during use that buttons seem to enable/disable as you go. Thats because you are restricted by design from using these buttons in the wrong order.
- the Consoles are divided in the middle. The left part is the log console, where you see reports of other operations completing successfuly (or throwing exceptions). The right side regularly refreshes during fuzzing displaying the output of afl-whatsup on the fuzzing directory. This allows you to see the relevant stats. For now the update interval is fixed at the default afl-fuzz value of 60 seconds, but im planning on either changing it to a faster one or to make it configurable.
