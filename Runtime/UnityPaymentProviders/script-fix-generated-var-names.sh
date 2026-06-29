#!/bin/bash
set -e

ORIGINAL_DIR="$(pwd)"
cd "$(dirname "$0")"
trap "cd -- \"$ORIGINAL_DIR\"" EXIT

echo "==> Fixing generated var names..."
if [[ "$(uname)" == "Darwin" ]]; then
    find ./Client -name "*.cs" -exec sed -i '' -f ./script-fix-generated-var-names.sed {} +
else
    find ./Client -name "*.cs" -exec sed -i -f ./script-fix-generated-var-names.sed {} +
fi
