#!/usr/bin/env bash
set -euo pipefail

image="${IMAGE:-ghcr.io/lancer1977/pathfinder-2e-srd-markdown-companion}"
version="${1:-latest}"
script_dir="$(cd "$(dirname "$0")" && pwd)"
# shellcheck source=/dev/null
source "$script_dir/scripts/lib/secrets.sh"
load_local_secrets_from_dir "${HOME}/.config/secrets" \
  ghcr.env \
  polyhydra.env
token="${GHCR_TOKEN:-${GITHUB_PACKAGES_TOKEN:-${GITHUB_TOKEN:-}}}"
actor="${GITHUB_ACTOR:-lancer1977}"

if [[ -z "$token" ]]; then
    echo "GHCR_TOKEN, GITHUB_PACKAGES_TOKEN, or GITHUB_TOKEN is required for GHCR login." >&2
    exit 1
fi

printf '%s' "$token" | docker login ghcr.io -u "$actor" --password-stdin

if [[ "${version}" == "latest" ]]; then
    tags=("${image}:latest")
else
    tags=("${image}:${version}" "${image}:latest")
fi

docker build -t "${tags[0]}" .

if [[ "${#tags[@]}" -gt 1 ]]; then
    docker tag "${tags[0]}" "${tags[1]}"
fi

for tag in "${tags[@]}"; do
    docker push "${tag}"
done
