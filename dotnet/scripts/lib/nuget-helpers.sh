#!/usr/bin/env bash
# Shared NuGet URL validation and XML helpers.
# Source from repo scripts: source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/lib/nuget-helpers.sh"

nuget_url_safe_display() {
  local url="$1"
  feed_url_safe_display="${url}"
  local _safe_rest="${feed_url_safe_display}"
  local _safe_scheme=""
  if [[ "${feed_url_safe_display}" == *"://"* ]]; then
    _safe_scheme="${feed_url_safe_display%%://*}"
    _safe_rest="${feed_url_safe_display#*://}"
  fi
  local _safe_auth="${_safe_rest%%[/?#]*}"
  if [[ "${_safe_auth}" == *"@"* ]]; then
    _safe_auth="${_safe_auth##*@}"
  fi
  if [[ -n "${_safe_scheme}" ]]; then
    feed_url_safe_display="${_safe_scheme}://${_safe_auth}"
  else
    feed_url_safe_display="${_safe_auth}"
  fi
  printf '%s' "${feed_url_safe_display}"
}

validate_nuget_feed_url() {
  local feed_url="$1"
  local safe_display="$2"
  local docs_ref="${3:-docs/ci.md}"
  local style="${4:-verify}"
  local script_name="${5:-}"

  if [[ ! "${feed_url}" =~ ^https://[^/[:space:]]+(/.*)?$ ]]; then
    echo "::error title=Invalid NuGet feed URL::NUGET_FEED_URL must be https with a valid host (got: ${safe_display}). See ${docs_ref}" >&2
    return 1
  fi

  fe_host="${feed_url#*://}"
  fe_host="${fe_host%%[/?#]*}"
  if [[ "${fe_host}" == *"@"* ]]; then
    echo "::error title=Invalid NuGet feed URL::NUGET_FEED_URL must not contain userinfo; use HTTPS with host only (got: ${safe_display}). See ${docs_ref}" >&2
    return 1
  fi
  if [[ "${fe_host}" == \[* ]]; then
    fe_host="${fe_host#\[}"
    fe_host="${fe_host%%\]*}"
  else
    fe_host="${fe_host%%:*}"
  fi
  if [[ -z "${fe_host}" ]]; then
    echo "::error title=Invalid NuGet feed URL::NUGET_FEED_URL must be https with a non-empty host (got: ${safe_display}). See ${docs_ref}" >&2
    return 1
  fi
  return 0
}

nuget_xml_escape() {
  local s="$1"
  local out="" c
  local -i i len=${#s}
  for ((i = 0; i < len; i++)); do
    c="${s:i:1}"
    case "$c" in
      '&') out+='&amp;' ;;
      '<') out+='&lt;' ;;
      '>') out+='&gt;' ;;
      '"') out+='&quot;' ;;
      "'") out+='&apos;' ;;
      *) out+="$c" ;;
    esac
  done
  printf '%s' "$out"
}

validate_nuget_source_key() {
  local k="$1"
  local script_name="${2:-}"
  if [[ -z "$k" ]]; then
    echo "${script_name}: package source key must be non-empty" >&2
    return 1
  fi
  if [[ ! "$k" =~ ^[A-Za-z_][A-Za-z0-9_.-]*$ ]]; then
    echo "${script_name}: invalid package source key: ${k}" >&2
    return 1
  fi
  return 0
}

nuget_config_merge_source_credentials() {
  local cfg_path="$1"
  local src_key="$2"
  local username="$3"
  local token="$4"
  local script_name="${5:-nuget_config_merge_source_credentials}"

  validate_nuget_source_key "$src_key" "$script_name" || return 1
  if grep -q '<packageSourceCredentials>' "$cfg_path"; then
    echo "${script_name}: ${cfg_path} already has packageSourceCredentials; refuse to overwrite" >&2
    return 1
  fi

  local un pt tmp cred_tmp
  un="$(nuget_xml_escape "$username")"
  pt="$(nuget_xml_escape "$token")"
  tmp="$(mktemp "${cfg_path}.XXXXXX")"
  cred_tmp="$(mktemp "${cfg_path}.cred.XXXXXX")"
  chmod 600 "$tmp" "$cred_tmp"

  {
    echo '  <packageSourceCredentials>'
    printf '    <%s>\n' "$src_key"
    printf '      <add key="Username" value="%s" />\n' "$un"
    printf '      <add key="ClearTextPassword" value="%s" />\n' "$pt"
    printf '    </%s>\n' "$src_key"
    echo '  </packageSourceCredentials>'
  } > "$cred_tmp"

  awk -v cred_file="$cred_tmp" '
    /<\/configuration>/ {
      while ((getline line < cred_file) > 0) { print line }
      close(cred_file)
    }
    { print }
  ' "$cfg_path" > "$tmp"
  rm -f "$cred_tmp"
  mv "$tmp" "$cfg_path"
  chmod 600 "$cfg_path"
}
