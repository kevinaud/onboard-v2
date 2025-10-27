#!/usr/bin/env bash
set -euo pipefail

script_name=$(basename "${BASH_SOURCE[0]:-$0}")
temp_dir=""
download_path=""
preserve_binary=false

log() {
	printf '%s\n' "$1"
}

log_error() {
	printf '%s: %s\n' "$script_name" "$1" >&2
}

require_command() {
	if ! command -v "$1" >/dev/null 2>&1; then
		log_error "Required command '$1' is not available on PATH."
		exit 1
	fi
}

is_truthy() {
	case "$1" in
		1|true|TRUE|True|yes|YES|Yes|on|ON|On) return 0 ;;
		*) return 1 ;;
	esac
}

cleanup() {
	if [[ "$preserve_binary" == true ]]; then
		return
	fi

	if [[ -n "$temp_dir" && -d "$temp_dir" ]]; then
		rm -rf "$temp_dir"
	fi
}

trap cleanup EXIT

repository_slug="${ONBOARD_REPOSITORY:-${GITHUB_REPOSITORY:-kevinaud/onboard-v2}}"
release_tag="${ONBOARD_RELEASE_TAG:-}"

if [[ -n "${ONBOARD_KEEP_DOWNLOADED_BINARY:-}" ]] && is_truthy "${ONBOARD_KEEP_DOWNLOADED_BINARY}"; then
	preserve_binary=true
fi

if [[ -z "$repository_slug" || ! "$repository_slug" =~ ^[^/]+/[^/]+$ ]]; then
	log_error "Repository slug '$repository_slug' is invalid. Expected format 'owner/repo'."
	exit 1
fi

require_command curl
require_command python3
require_command uname

kernel_name=$(uname -s)
machine_arch=$(uname -m)

case "$kernel_name" in
	Darwin)
		os_token="osx"
		;;
	Linux)
		os_token="linux"
		;;
	*)
		log_error "Unsupported operating system '$kernel_name'."
		exit 1
		;;
esac

case "$machine_arch" in
	x86_64|amd64)
		arch_token="x64"
		;;
	arm64|aarch64)
		arch_token="arm64"
		;;
	*)
		log_error "Unsupported CPU architecture '$machine_arch'."
		exit 1
		;;
esac

if [[ "$os_token" == linux && "$arch_token" != x64 ]]; then
	log_error "Linux builds are currently published for x64 hosts only."
	exit 1
fi

if [[ "$os_token" == osx && "$arch_token" != x64 && "$arch_token" != arm64 ]]; then
	log_error "macOS builds are available for x64 and arm64 hosts only."
	exit 1
fi

asset_name="Onboard-${os_token}-${arch_token}"

api_base="https://api.github.com/repos/${repository_slug}/releases"
if [[ -n "$release_tag" ]]; then
	release_url="${api_base}/tags/${release_tag}"
else
	release_url="${api_base}/latest"
fi

curl_args=(
	--fail
	--silent
	--show-error
	--location
	-H "Accept: application/vnd.github+json"
	-H "User-Agent: OnboardSetupScript"
)

if [[ -n "${GITHUB_TOKEN:-}" ]]; then
	curl_args+=(-H "Authorization: Bearer ${GITHUB_TOKEN}")
fi

log "Fetching release metadata from ${release_url}"
release_payload=$(curl "${curl_args[@]}" "$release_url")

download_url=$(python3 - "$asset_name" <<'PY' <<<"$release_payload"
import json
import sys

data = json.load(sys.stdin)
asset_name = sys.argv[1]

assets = data.get("assets", [])
for asset in assets:
		if asset.get("name") == asset_name:
				url = asset.get("browser_download_url")
				if not url:
						break
				print(url)
				sys.exit(0)

available = [a.get("name") for a in assets if a.get("name")]
if available:
		raise SystemExit(f"Asset '{asset_name}' not found. Available assets: {', '.join(available)}")

raise SystemExit(f"Asset '{asset_name}' not found in release response.")
PY
)

if [[ -z "$download_url" ]]; then
	log_error "Failed to resolve download URL for asset '${asset_name}'."
	exit 1
fi

temp_dir=$(mktemp -d)
download_path="${temp_dir}/${asset_name}"

log "Downloading ${asset_name} to temporary location"
curl "${curl_args[@]}" --output "$download_path" "$download_url"
chmod +x "$download_path"

log "Launching downloaded Onboard binary"
"$download_path" "$@"

if [[ "$preserve_binary" == true ]]; then
	log "Binary preserved at ${download_path}"
fi
