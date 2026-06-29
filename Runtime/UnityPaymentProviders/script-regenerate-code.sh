#!/bin/bash
set -e

if [ -z "$1" ]; then
    echo "Usage: $0 <path/to/generator.jar>"
    exit 1
fi

if [ ! -f "$1" ]; then
    echo "ERROR: Generator jar not found: $1"
    exit 1
fi

# Resolve the jar's full path before cd-ing, so relative paths passed by the caller remain valid.
JAR_DIR="$(cd "$(dirname "$1")" && pwd)"
JAR_NAME="$(basename "$1")"
JAR_PATH="$JAR_DIR/$JAR_NAME"

ORIGINAL_DIR="$(pwd)"
cd "$(dirname "$0")"
trap "cd -- \"$ORIGINAL_DIR\"" EXIT

echo "==> Generating client..."
java -jar "$JAR_PATH" generate -c ./configuration.yaml

echo "==> Replacing Client..."
rm -rf ./Client && mkdir ./Client && mv ./tmp-client/com.unity.purchasing.paymentProviders/Runtime/* ./Client/ && rm -rf ./tmp-client

echo "==> Finding Unity 6000.0+ editor..."

# UNITY_PATH overrides OS detection entirely — point it at a Unity 6000+ binary.
if [ -n "$UNITY_PATH" ]; then
    if [ ! -x "$UNITY_PATH" ]; then
        echo "ERROR: UNITY_PATH is set to '$UNITY_PATH' but the file does not exist or is not executable."
        exit 1
    fi
    UNITY_BINARY="$UNITY_PATH"
    echo "    Using UNITY_PATH override: $UNITY_BINARY"
else
    case "$(uname -s)" in
        Darwin)
            HUB_DIR="/Applications/Unity/Hub/Editor"
            BIN_SUFFIX="Unity.app/Contents/MacOS/Unity"
            ;;
        Linux)
            # Prefer a native Linux Hub install; fall back to the Windows Hub
            # via the WSL /mnt/c mount.
            if [ -d "$HOME/Unity/Hub/Editor" ]; then
                HUB_DIR="$HOME/Unity/Hub/Editor"
                BIN_SUFFIX="Editor/Unity"
            else
                HUB_DIR="/mnt/c/Program Files/Unity/Hub/Editor"
                BIN_SUFFIX="Editor/Unity.exe"
            fi
            ;;
        MINGW*|MSYS*|CYGWIN*)
            HUB_DIR="/c/Program Files/Unity/Hub/Editor"
            BIN_SUFFIX="Editor/Unity.exe"
            ;;
        *)
            echo "ERROR: Unsupported OS '$(uname -s)'. Set UNITY_PATH to a Unity 6000.0+ binary, or extend this script."
            exit 1
            ;;
    esac

    if [ ! -d "$HUB_DIR" ]; then
        echo "ERROR: Unity Hub Editor directory not found at '$HUB_DIR'."
        echo "       Install Unity Hub, or set UNITY_PATH to a Unity 6000.0+ binary directly."
        exit 1
    fi

    UNITY_VERSION=$(ls "$HUB_DIR" | grep '^6000' | sort | tail -1)
    if [ -z "$UNITY_VERSION" ]; then
        echo "ERROR: No 6000.0+ Unity editor found in '$HUB_DIR'."
        echo "       Install one via Unity Hub, or set UNITY_PATH directly."
        exit 1
    fi
    UNITY_BINARY="$HUB_DIR/$UNITY_VERSION/$BIN_SUFFIX"
    echo "    Using Unity $UNITY_VERSION at $UNITY_BINARY"
fi

echo "==> Generating .meta files (exit code 1 is expected)..."
"$UNITY_BINARY" -batchMode -quit -projectPath ../../../../Test-Projects/2023 || true

echo "==> Restoring .meta files..."
# --diff-filter=M: only restore modified meta files (not new or deleted ones).
# -z/--null: null-delimit output to handle filenames with spaces.
# Pipe directly to avoid bash $() stripping null bytes; xargs -0 reads null-delimited input.
git diff -z --name-only --diff-filter=M --relative -- 'Client/**/*.meta' 'Client/*.meta' | xargs -0 git checkout --

echo "==> Fixing generated var names..."
./script-fix-generated-var-names.sh

echo ""
echo "==> Remaining changes in Client/ (review for spurious diffs before committing):"
git status Client/ --short
echo ""
echo "    If all changes look correct, stage them with: git add Client/"
