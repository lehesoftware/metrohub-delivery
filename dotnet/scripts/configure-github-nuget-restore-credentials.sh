#!/usr/bin/env bash
# Copy repo nuget.config to an ephemeral file, set the github feed URL, and inject credentials in-file.
# Token precedence: NUGET_PACKAGES_READ_TOKEN → GITHUB_PACKAGES_NUGET_TOKEN / NUGET_AUTH_TOKEN.
set -euo pipefail

_script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
source "${_script_dir}/lib/nuget-helpers.sh"

repo_config="${NUGET_REPO_CONFIG:-nuget.config}"
token="${NUGET_PACKAGES_READ_TOKEN:-${GITHUB_PACKAGES_NUGET_TOKEN:-${NUGET_AUTH_TOKEN:-}}}"
username="${NUGET_GITHUB_USERNAME:-github}"

if [[ ! -f "${repo_config}" ]]; then
  echo "$0: missing ${repo_config}" >&2
  exit 1
fi
if [[ -z "${token}" ]]; then
  echo "$0: set NUGET_PACKAGES_READ_TOKEN, GITHUB_PACKAGES_NUGET_TOKEN, or NUGET_AUTH_TOKEN" >&2
  exit 1
fi

echo "::add-mask::${token}"

owner="${NUGET_GITHUB_OWNER:-lehesoftware}"
feed="${NUGET_FEED_URL:-https://nuget.pkg.github.com/${owner}/index.json}"
safe="$(nuget_url_safe_display "${feed}")"
validate_nuget_feed_url "${feed}" "${safe}" docs/ci.md || exit 1

_base="${RUNNER_TEMP:-${TMPDIR:-/tmp}}"
nuget_cfg="$(mktemp "${_base}/nuget-restore.XXXXXX")"
chmod 600 "${nuget_cfg}"
cp "${repo_config}" "${nuget_cfg}"

dotnet nuget update source github \
  --source "${feed}" \
  --configfile "${nuget_cfg}"

nuget_config_merge_source_credentials "${nuget_cfg}" github "${username}" "${token}" "$0" || exit 1

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  echo "configfile=${nuget_cfg}" >> "${GITHUB_OUTPUT}"
fi

echo "::notice::NuGet restore credentials configured in ephemeral config (${safe})"
echo "${nuget_cfg}"
