#!/usr/bin/env bash
set -euo pipefail

ref="${1:-main}"
gh workflow run deploy.yml --ref "$ref"
