#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'EOF'
Usage: release-tag.sh [--version vX.Y.Z]
Creates an annotated tag on main and pushes it to origin to trigger the release pipeline.
EOF
}

VERSION=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --version)
      shift || { echo "--version requires an argument" >&2; exit 1; }
      VERSION="${1}";
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ -z "$VERSION" ]]; then
  read -rp "Enter release version (e.g. v0.3.1): " VERSION
fi

if [[ ! "$VERSION" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "Version must match pattern v<major>.<minor>.<patch>, for example v0.3.1" >&2
  exit 1
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$script_dir"

git switch main
git pull --ff-only
git tag -a "$VERSION" -m "$VERSION"

git push origin "$VERSION"
