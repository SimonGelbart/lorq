#!/usr/bin/env sh
set -eu
mkdir -p .agent-prep
printf '{"prepared": true}\n' > .agent-prep/example.json
