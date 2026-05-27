#!/usr/bin/env bash
# install-claude-code-secure.sh
#
# Install Claude Code securely without the "curl | bash" or
# "curl | gpg --import" anti-patterns.
#
# Principles:
#   1. Every download is saved to a file before verification — never piped.
#   2. The trust anchor (GPG fingerprint) is hardcoded in this script.
#   3. Nothing downloaded is executed or trusted until verification passes.
#
# Usage:
#   # Review this script first, then run it.
#   chmod +x install-claude-code-secure.sh
#   ./install-claude-code-secure.sh [VERSION]
#
#   If VERSION is omitted, the latest "stable" channel release is used.
#   Example: ./install-claude-code-secure.sh 2.1.89

set -euo pipefail

# ============================================================
# Configuration: hardcoded trust anchor
# ============================================================

# Anthropic's Claude Code release signing key fingerprint
# https://code.claude.com/docs/en/setup#binary-integrity-and-code-signing
EXPECTED_FINGERPRINT="31DDDE24DDFAB679F42D7BD2BAA929FF1A7ECACE"

# Download sources
KEY_URL="https://downloads.claude.ai/keys/claude-code.asc"
RELEASES_BASE="https://downloads.claude.ai/claude-code-releases"

# Installation target
INSTALL_DIR="${HOME}/.local/bin"

# ============================================================
# Utilities
# ============================================================

readonly WORK_DIR="$(mktemp -d)"
trap 'rm -rf "${WORK_DIR}"' EXIT

log()  { printf '\033[1;34m==>\033[0m %s\n' "$*"; }
err()  { printf '\033[1;31mERROR:\033[0m %s\n' "$*" >&2; }
die()  { err "$@"; exit 1; }

require_cmd() {
    command -v "$1" >/dev/null 2>&1 || die "'$1' not found. Please install it and try again."
}

# ============================================================
# Prerequisite checks
# ============================================================

require_cmd curl
require_cmd gpg
require_cmd sha256sum
require_cmd jq

# ============================================================
# Platform detection
# ============================================================

detect_platform() {
    local os arch

    case "$(uname -s)" in
        Linux)  os="linux" ;;
        Darwin) os="darwin" ;;
        *)      die "Unsupported OS: $(uname -s)" ;;
    esac

    case "$(uname -m)" in
        x86_64|amd64)   arch="x64" ;;
        aarch64|arm64)  arch="arm64" ;;
        *)              die "Unsupported architecture: $(uname -m)" ;;
    esac

    # Detect musl libc (Alpine, etc.)
    local libc=""
    if [ "$os" = "linux" ]; then
        if ldd --version 2>&1 | grep -qi musl; then
            libc="-musl"
        fi
    fi

    echo "${os}-${arch}${libc}"
}

PLATFORM="$(detect_platform)"
log "Platform: ${PLATFORM}"

# ============================================================
# Step 1: Download GPG key and verify fingerprint BEFORE import
# ============================================================

log "Downloading GPG public key..."
curl -fsSL -o "${WORK_DIR}/claude-code.asc" "${KEY_URL}"

# Inspect the key without importing it.
# Uses --import-options show-only (GPG 2.1+) instead of --import.
log "Verifying key fingerprint without importing..."
KEY_INFO="$(gpg --batch --with-colons --import-options show-only --import "${WORK_DIR}/claude-code.asc" 2>/dev/null)"

# Extract fingerprint from the fpr record
ACTUAL_FINGERPRINT="$(echo "${KEY_INFO}" | awk -F: '/^fpr:/ { print $10; exit }')"

if [ -z "${ACTUAL_FINGERPRINT}" ]; then
    die "Failed to extract fingerprint. Your GPG version may be too old (need 2.1+)."
fi

log "Downloaded key fingerprint: ${ACTUAL_FINGERPRINT}"
log "Expected fingerprint:       ${EXPECTED_FINGERPRINT}"

if [ "${ACTUAL_FINGERPRINT}" != "${EXPECTED_FINGERPRINT}" ]; then
    die "Fingerprint mismatch! The key may have been tampered with."
fi

log "Fingerprint matches. Importing key."
gpg --batch --import "${WORK_DIR}/claude-code.asc"

# ============================================================
# Step 2: Determine version
# ============================================================

VERSION="${1:-}"

if [ -z "${VERSION}" ]; then
    log "No version specified. Resolving latest stable version..."
    curl -fsSL -o "${WORK_DIR}/version.txt" "${RELEASES_BASE}/stable/version"
    VERSION="$(cat "${WORK_DIR}/version.txt" | tr -d '[:space:]')"
    if [ -z "${VERSION}" ]; then
        die "Failed to resolve the latest stable version."
    fi
    log "Latest stable version: ${VERSION}"
fi

# ============================================================
# Step 3: Download manifest and signature, verify GPG signature
# ============================================================

log "Downloading manifest.json..."
curl -fsSL -o "${WORK_DIR}/manifest.json" "${RELEASES_BASE}/${VERSION}/manifest.json"

log "Downloading manifest.json.sig..."
curl -fsSL -o "${WORK_DIR}/manifest.json.sig" "${RELEASES_BASE}/${VERSION}/manifest.json.sig"

log "Verifying GPG signature on manifest.json..."
if ! gpg --batch --verify "${WORK_DIR}/manifest.json.sig" "${WORK_DIR}/manifest.json" 2>&1; then
    die "Manifest signature verification failed."
fi

log "Signature OK."

# ============================================================
# Step 4: Extract platform binary info from manifest.json
# ============================================================

EXPECTED_CHECKSUM="$(jq -r ".platforms.\"${PLATFORM}\".checksum // empty" "${WORK_DIR}/manifest.json")"
BINARY_URL="$(jq -r ".platforms.\"${PLATFORM}\".url // empty" "${WORK_DIR}/manifest.json")"

if [ -z "${EXPECTED_CHECKSUM}" ] || [ -z "${BINARY_URL}" ]; then
    # If the url field is missing, try the filename field as a fallback
    if [ -z "${EXPECTED_CHECKSUM}" ]; then
        die "No checksum found for platform '${PLATFORM}' in manifest.json."
    fi
    # Fallback: construct URL from the filename field
    BINARY_FILENAME="$(jq -r ".platforms.\"${PLATFORM}\".filename // empty" "${WORK_DIR}/manifest.json")"
    if [ -n "${BINARY_FILENAME}" ]; then
        BINARY_URL="${RELEASES_BASE}/${VERSION}/${BINARY_FILENAME}"
    else
        die "Cannot determine download URL for platform '${PLATFORM}'. Inspect manifest.json manually."
    fi
fi

log "Download URL: ${BINARY_URL}"
log "Expected SHA256: ${EXPECTED_CHECKSUM}"

# ============================================================
# Step 5: Download binary and verify SHA256 checksum
# ============================================================

log "Downloading binary..."
curl -fsSL -o "${WORK_DIR}/claude" "${BINARY_URL}"

log "Verifying SHA256 checksum..."
ACTUAL_CHECKSUM="$(sha256sum "${WORK_DIR}/claude" | awk '{ print $1 }')"

if [ "${ACTUAL_CHECKSUM}" != "${EXPECTED_CHECKSUM}" ]; then
    err "Expected: ${EXPECTED_CHECKSUM}"
    err "Actual:   ${ACTUAL_CHECKSUM}"
    die "Checksum mismatch! The binary may have been tampered with."
fi

log "Checksum OK."

# ============================================================
# Step 6: Install
# ============================================================

mkdir -p "${INSTALL_DIR}"
chmod +x "${WORK_DIR}/claude"
mv "${WORK_DIR}/claude" "${INSTALL_DIR}/claude"

log "Installed: ${INSTALL_DIR}/claude"

# Check if INSTALL_DIR is in PATH
if ! echo "${PATH}" | tr ':' '\n' | grep -qx "${INSTALL_DIR}"; then
    log ""
    log "WARNING: ${INSTALL_DIR} is not in your PATH."
    log "  Add the following to your .bashrc / .zshrc:"
    log "  export PATH=\"${INSTALL_DIR}:\${PATH}\""
fi

"${INSTALL_DIR}/claude" --version
log "Done."
