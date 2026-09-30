#!/bin/sh
# The Linux run of the wiki's verification (2026-09-29, WSL 2, Ubuntu 26.04.1, which has no .NET and no libicu).
# No system package is installed: a user-level .NET 10 SDK from dotnet-install.sh, and libicu fetched with
# `apt-get download` and unpacked with `dpkg -x`, both into the scratch folder. The tr-TR cases need ICU, so
# invariant globalization mode is not an option.
#   WW_SCRATCH=/mnt/c/.../scratch REPO=/mnt/d/.../IsImageUrlDotNet sh 2026-09-29-linux-run.sh
# Outputs: $WW_SCRATCH/verify/out.linux.txt and $WW_SCRATCH/golden/golden-replay.linux.out.txt.
set -e
W="${WW_SCRATCH:?set WW_SCRATCH to a scratch folder}"
REPO="${REPO:?set REPO to the repository clone}"
NOTES="$REPO/ai-docs/notes"
mkdir -p "$W/icu-deb" "$W/tmp" "$W/verify" "$W/golden"
if [ ! -x "$W/dotnet/dotnet" ]; then
  curl -sSL https://dot.net/v1/dotnet-install.sh -o "$W/dotnet-install.sh"
  bash "$W/dotnet-install.sh" --channel 10.0 --install-dir "$W/dotnet" --no-path   # the script needs bash, not sh
fi
if [ ! -d "$W/icu" ]; then
  PKG=$(apt-cache search --names-only '^libicu[0-9]+$' | awk '{print $1}' | sort -V | tail -1)
  (cd "$W/icu-deb" && apt-get download "$PKG" && dpkg -x ./"$PKG"_*.deb "$W/icu")
fi
export DOTNET_ROOT="$W/dotnet" PATH="$W/dotnet:/usr/bin:/bin" DOTNET_CLI_HOME="$W/home" NUGET_PACKAGES="$W/nuget"
export TMPDIR="$W/tmp" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 LD_LIBRARY_PATH="$W/icu/usr/lib/x86_64-linux-gnu"
unset HTTP_PROXY HTTPS_PROXY http_proxy https_proxy ALL_PROXY NO_PROXY no_proxy
cp "$NOTES/2026-09-29-wiki-verify.cs" "$W/verify/wiki-verify.cs"
dotnet run "$W/verify/wiki-verify.cs" -- "$W/verify" > "$W/verify/out.linux.txt"
python3 "$NOTES/2026-09-29-golden-replay.py" "$REPO" "$W/golden" > "$W/golden/golden-replay.linux.out.txt"
