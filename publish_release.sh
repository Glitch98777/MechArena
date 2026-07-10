#!/usr/bin/env bash
# Publish the latest Build/MechArena.apk as a NEW GitHub release.
# Run after every completed build:  bash publish_release.sh  [optional notes]
set -e
cd "$(dirname "$0")"

APK="Build/MechArena.apk"
[ -f "$APK" ] || { echo "No APK at $APK — build first."; exit 1; }

# incrementing build number (machine-local)
NF=".buildnum"
N=$(( $(cat "$NF" 2>/dev/null || echo 0) + 1 ))
echo "$N" > "$NF"
TAG="v1.0.$N"

NOTE="${1:-New build.}"
{
  echo "$NOTE"
  echo ""
  echo "**Download \`MechArena.apk\` below and tap to install** (arm64, release-signed, Android 7+)."
  echo ""
  echo "---"
  sed -n '1,30p' CHANGELOG.md
} > .relnotes.tmp

# push any committed source changes first (ignore if nothing/no upstream yet)
git push origin HEAD 2>/dev/null || true

gh release create "$TAG" "$APK" --title "Mech Arena $TAG" --notes-file .relnotes.tmp --latest
rm -f .relnotes.tmp
echo "Published release $TAG"
