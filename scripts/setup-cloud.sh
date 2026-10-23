#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
export DOTNET_CLI_HOME="$PWD/.tools/cli"
export NUGET_PACKAGES="$PWD/.tools/nuget"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
mkdir -p .tools
if [[ ! -x .tools/dotnet/dotnet ]]; then
    # Official Microsoft archive, pinned SDK and SHA-512, TLS verification enabled.
    archive="$PWD/.tools/dotnet-sdk.tar.gz"
    curl --fail --location --retry 2 'https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.100/dotnet-sdk-10.0.100-linux-x64.tar.gz' -o "$archive"
    echo "f78dbac30c9af2230d67ff5c224de3a5dbf63f8a78d1c206594dedb80e6909d2cc8a9d865d5105c72c2fd2aa266fc0c6c77dedac60408cbccf272b116bd11b07  $archive" | sha512sum --check
    mkdir -p .tools/dotnet
    tar -xzf "$archive" -C .tools/dotnet
    rm "$archive"
fi
export PATH="$PWD/.tools/dotnet:$PATH"
dotnet restore tests/Asuka.Tests --locked-mode
dotnet restore tests/Asuka.Windows.Tests --locked-mode
dotnet build src/Asuka.Windows -c Release --no-restore
dotnet test tests/Asuka.Tests -c Release --no-restore
python3 scripts/fetch-models.py
dotnet restore tests/Asuka.Models.Tests --locked-mode
dotnet test tests/Asuka.Models.Tests -c Release --no-restore
