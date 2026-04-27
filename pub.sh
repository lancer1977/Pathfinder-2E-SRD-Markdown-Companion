#!/usr/bin/env bash
set -euo pipefail

image="${IMAGE:-ghcr.io/lancer1977/pathfinder-2e-srd-markdown-companion}"
version="${1:-latest}"

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
