# syntax=docker/dockerfile:1

# Use Ubuntu 22.04 LTS as the base image, its widely supported and its Linux so it will work, hopefully
FROM ubuntu:22.04

# Prevent interactive prompts during package installation, because this is getting built automatically
ENV DEBIAN_FRONTEND=noninteractive
ENV TZ=Etc/UTC

# ----------------------------------------------------------------------
# 1. Install basic tools needed to download stuff(wget, curl, gnupg)
# ----------------------------------------------------------------------
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        wget \
        curl \
        gnupg \
		ca-certificates \
    && rm -rf /var/lib/apt/lists/*
#NOTE: Apparently docker layers make it so that lists and such are just invisible further on, and add unnecessary weight for no reason.
#      So we delete it.

# ----------------------------------------------------------------------
# 2. Add the official LLVM repository (for llvm-18, apparently you only have sources for llvm-14 by default) and install all AFL++ prerequisites. This has the pleasant side-effect of downloading most common tools aswell (gdb, gcc, python, stuff like that)
# ----------------------------------------------------------------------
# Download and trust the LLVM GPG key
RUN wget -O- https://apt.llvm.org/llvm-snapshot.gpg.key | tee /etc/apt/trusted.gpg.d/llvm.asc && \
    # Add the LLVM 18 repository for Ubuntu 22.04 (jammy)
    echo "deb http://apt.llvm.org/jammy/ llvm-toolchain-jammy-18 main" >> /etc/apt/sources.list.d/llvm.list && \
    # Update package lists (now including LLVM 18 packages) and install everything
    apt-get update && apt-get install -y \
        build-essential \
		gdb \
		libc6-dbg \
        python3-dev \
        automake \
        cmake \
        git \
        flex \
        bison \
        libglib2.0-dev \
        libpixman-1-dev \
        python3-setuptools \
        cargo \
		screen \
        libgtk-3-dev \
        lld-18 \
        llvm-18 \
        llvm-18-dev \
        clang-18 \
		libasan6 \
        gcc-12-plugin-dev \
        libstdc++-12-dev \
        meson \
        ninja-build \
        cpio \
        libcapstone-dev \
        python3-pip \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# ----------------------------------------------------------------------
# 3. Build and install AFL++ from source (so you can actually use it)
# ----------------------------------------------------------------------
RUN git clone https://github.com/AFLplusplus/AFLplusplus.git /opt/AFLplusplus && \
    cd /opt/AFLplusplus && \
    git submodule update --init && \
    make distrib && \
    make install && \
    cd / && rm -rf /opt/AFLplusplus

# ----------------------------------------------------------------------
# 4. Install .NET SDK 8.0 (LTS) – because its LTS and its whats mentioned on the sharpfuzz website. .NET is whats running c# in case you forget.
# ----------------------------------------------------------------------
# Add Microsoft's package repository for Ubuntu 22.04
RUN wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb && \
    dpkg -i packages-microsoft-prod.deb && \
    rm packages-microsoft-prod.deb && \
    apt-get update && \
    apt-get install -y dotnet-sdk-8.0 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Set environment variables so that .NET global tools are in PATH and telemetry is disabled
ENV PATH="${PATH}:/root/.dotnet/tools"
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# ----------------------------------------------------------------------
# 5. Install SharpFuzz as a .NET global tool
# ----------------------------------------------------------------------
RUN dotnet tool install --global SharpFuzz.CommandLine

# ----------------------------------------------------------------------
# 6. Install afl-utils (experimental branch) for crash deduplication and corpus minimization
# ----------------------------------------------------------------------

# Clone afl-utils repository and switch to experimental branch
RUN git clone https://gitlab.com/rc0r/afl-utils.git /opt/afl-utils && \
    cd /opt/afl-utils && \
    git checkout experimental && \
    git submodule update --init --recursive

# Install afl-utils using the install.py script
RUN cd /opt/afl-utils && \
    python3 setup.py install

# Configure GDB to load exploitable.py (required for afl-collect crash analysis)
RUN find /usr/local/lib/python3.10/dist-packages/ -name exploitable.py \
        -exec echo "source {}" >> /root/.gdbinit \;
		
# ----------------------------------------------------------------------
# 7. Install dependencies for afl-cov-fast (coverage analysis)
# ----------------------------------------------------------------------
RUN apt-get update && apt-get install -y --no-install-recommends \
        lcov \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Create symlinks so that afl-cov-fast finds llvm-profdata and llvm-cov
RUN ln -s /usr/bin/llvm-profdata-18 /usr/bin/llvm-profdata && \
    ln -s /usr/bin/llvm-cov-18 /usr/bin/llvm-cov

# ----------------------------------------------------------------------
# 8. Install afl-cov-fast from source (for code coverage visualization)
# ----------------------------------------------------------------------
RUN git clone --recursive https://github.com/airbus-seclab/afl-cov-fast.git /opt/afl-cov-fast && \
    cd /opt/afl-cov-fast && \
    pip3 install -r requirements.txt
	
# ----------------------------------------------------------------------
# 9. Install CrossRename for Windows-compatible filename conversion (afl generates bad names)
# ----------------------------------------------------------------------
RUN pip3 install CrossRename

# ----------------------------------------------------------------------
# 10. Set a working directory (optional, just starting folder)
# ----------------------------------------------------------------------
WORKDIR /workspace

# ----------------------------------------------------------------------
# 11. Default command (can be overridden, we just run the console. Without additional input this actually kills the container instantly)
# ----------------------------------------------------------------------
CMD ["/bin/bash"]