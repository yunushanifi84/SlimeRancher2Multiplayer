#!/usr/bin/env bash
# Copies the dependency DLLs needed to build SR2MP from a local Slime Rancher 2 +
# MelonLoader install into SR2MP/libraries/. These DLLs are game/MelonLoader binaries
# and are intentionally git-ignored, so every fork must regenerate them locally.
#
# Usage:
#   ./setup-libraries.sh ["/path/to/Slime Rancher 2"]
# Defaults to the standard Steam install path if no argument is given.
set -euo pipefail

GAME="${1:-/c/Program Files (x86)/Steam/steamapps/common/Slime Rancher 2}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
LIB="$SCRIPT_DIR/SR2MP/libraries"

IL2CPP="$GAME/MelonLoader/Il2CppAssemblies"
NET6="$GAME/MelonLoader/net6"
MODS="$GAME/Mods"

if [ ! -d "$IL2CPP" ]; then
  echo "ERROR: Il2CppAssemblies not found at: $IL2CPP" >&2
  echo "Run the game once with MelonLoader installed so it generates the interop assemblies." >&2
  exit 1
fi

mkdir -p "$LIB"

# 1. All IL2CPP interop assemblies (UnityEngine.*, Assembly-CSharp, Il2Cpp*, Unity.*)
cp "$IL2CPP/"*.dll "$LIB/"

# 2. MelonLoader runtime + Harmony + Il2CppInterop + compression/asset helpers
for f in \
  MelonLoader.dll \
  0Harmony.dll \
  Il2CppInterop.Runtime.dll \
  Il2CppInterop.Common.dll \
  AssetsTools.NET.dll \
  UnityEngine.Il2CppAssetBundleManager.dll \
  UnityEngine.Il2CppImageConversionManager.dll
do
  cp "$NET6/$f" "$LIB/"
done

# 3. Starlight (SR2E) expansion framework
if [ -f "$MODS/Starlight.dll" ]; then
  cp "$MODS/Starlight.dll" "$LIB/"
else
  echo "WARNING: Starlight.dll not found in $MODS — install the Starlight mod." >&2
fi

echo "Copied $(ls "$LIB"/*.dll | wc -l | tr -d ' ') DLLs into $LIB"
echo "Now run: dotnet build SR2MP/SR2MP.csproj -c Debug"
