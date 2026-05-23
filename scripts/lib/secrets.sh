#!/usr/bin/env bash
set -euo pipefail

load_local_secrets_from_dir() {
  local secrets_dir="$1"
  shift || true

  local secret_file
  for secret_file in "$@"; do
    [[ -n "$secret_file" ]] || continue
    if [[ -f "$secrets_dir/$secret_file" ]]; then
      # shellcheck source=/dev/null
      source "$secrets_dir/$secret_file"
    fi
  done
}

require_env() {
  local var_name="$1"
  local value="${!var_name:-}"
  if [[ -z "$value" ]]; then
    echo "ERROR: ${var_name} is required." >&2
    exit 1
  fi
}
