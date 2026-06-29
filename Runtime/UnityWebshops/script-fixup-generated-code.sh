#!/bin/bash
set -e

ORIGINAL_DIR="$(pwd)"
cd "$(dirname "$0")"
trap "cd -- \"$ORIGINAL_DIR\"" EXIT

if [[ "$(uname)" == "Darwin" ]]; then
    SED_INPLACE=(sed -i '')
else
    SED_INPLACE=(sed -i)
fi

echo "==> Wrapping unconditional AddParamsToQueryParams calls with IsNullOrEmpty checks..."
# Sliding two-line window via N/P/D. Matches a `var XStringValue = ...;` line
# immediately followed by `queryParams = AddParamsToQueryParams(..., XStringValue);`
# (var name reused via backreference \3). Idempotent: after wrapping, the line
# following the `var` declaration is `if (!string.IsNullOrEmpty(...))`, so
# re-running finds nothing to substitute. `$!N` skips the N on the last input
# line (otherwise sed drops the pattern space on N-EOF instead of printing it).
# Use `\;` (one sed invocation per file) — `+` would batch multiple files into
# a single sed run, and pattern-space + the `$` address would leak across file
# boundaries.
WRAP_PROG='$!N
s|^\([[:space:]]*\)\(var \([A-Za-z_][A-Za-z0-9_]*StringValue\) = [^;]*;\)\n[[:space:]]*queryParams = AddParamsToQueryParams(queryParams, "\([^"][^"]*\)", \3);|\1\2\
\1if (!string.IsNullOrEmpty(\3))\
\1{\
\1    queryParams = AddParamsToQueryParams(queryParams, "\4", \3);\
\1}|
P
D'

find ./Client -name "*.cs" -exec "${SED_INPLACE[@]}" -e "$WRAP_PROG" {} \;
