#!/usr/bin/env bash
set -euo pipefail

rid="${1:-}"
case "$rid" in
  win-x64|win-arm64|osx-x64|osx-arm64|linux-x64|linux-arm64) ;;
  *)
  echo "usage: ./scripts/publish.sh <win-x64|win-arm64|osx-x64|osx-arm64|linux-x64|linux-arm64>" >&2
  exit 2
  ;;
esac

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
output="$repo_root/artifacts/$rid"
rm -rf "$output"

dotnet publish "$repo_root/src/Vers.Gui/Vers.Gui.csproj" \
  --configuration Release \
  --runtime "$rid" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishDir="$output/"

find "$output" -type f -name '*.pdb' -delete
