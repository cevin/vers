#!/usr/bin/env bash
set -euo pipefail

if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "mac.sh must be run on macOS." >&2
  exit 2
fi

case "$(uname -m)" in
  arm64) rid="osx-arm64" ;;
  x86_64) rid="osx-x64" ;;
  *)
    echo "Unsupported macOS architecture: $(uname -m)" >&2
    exit 2
    ;;
esac

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
output="$repo_root/artifacts/macos"
app="$output/Vers.app"
contents="$app/Contents"
publish_dir="$(mktemp -d "${TMPDIR:-/tmp}/vers-publish.XXXXXX")"
trap 'rm -rf "$publish_dir"' EXIT

rm -rf "$output"
mkdir -p "$contents/MacOS" "$contents/Resources"

dotnet publish "$repo_root/src/Vers.Gui/Vers.Gui.csproj" \
  --configuration Release \
  --runtime "$rid" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishDir="$publish_dir/"

cp "$publish_dir/ver" "$contents/MacOS/ver"
chmod +x "$contents/MacOS/ver"
cp "$repo_root/src/Vers.Gui/Assets/AppIcon.icns" "$contents/Resources/AppIcon.icns"

plist="$contents/Info.plist"
/usr/libexec/PlistBuddy -c "Clear dict" "$plist"
/usr/libexec/PlistBuddy -c "Add :CFBundleName string Vers" "$plist"
/usr/libexec/PlistBuddy -c "Add :CFBundleDisplayName string Vers" "$plist"
/usr/libexec/PlistBuddy -c "Add :CFBundleIdentifier string tech.acmestudio.vers" "$plist"
/usr/libexec/PlistBuddy -c "Add :CFBundleExecutable string ver" "$plist"
/usr/libexec/PlistBuddy -c "Add :CFBundleIconFile string AppIcon" "$plist"
/usr/libexec/PlistBuddy -c "Add :CFBundlePackageType string APPL" "$plist"
/usr/libexec/PlistBuddy -c "Add :CFBundleShortVersionString string 0.1.0" "$plist"
/usr/libexec/PlistBuddy -c "Add :CFBundleVersion string 1" "$plist"
/usr/libexec/PlistBuddy -c "Add :LSMinimumSystemVersion string 12.0" "$plist"
/usr/libexec/PlistBuddy -c "Add :NSHighResolutionCapable bool true" "$plist"

echo "Created $app"
