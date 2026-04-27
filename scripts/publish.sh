#!/usr/bin/env bash
set -euo pipefail

dotnet publish src/PathfinderRagChatUi -c Release -o artifacts/publish

