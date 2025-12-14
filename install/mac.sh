#!/usr/bin/env bash
set -euo pipefail

BASE_URL="https://github.com/rizwan3d/Ivory/releases/latest/download"
INSTALL_DIR="${HOME}/.ivory/bin"

if ! command -v curl >/dev/null 2>&1; then
  echo "curl is required to download Ivory." >&2
  exit 1
fi

if ! command -v tar >/dev/null 2>&1; then
  echo "tar is required to extract the Ivory archive." >&2
  exit 1
fi

ARCH="$(uname -m)"
case "${ARCH}" in
  arm64) FILE="iv-osx-arm64.tar.gz" ;;
  x86_64|amd64) FILE="iv-osx-x64.tar.gz" ;;
  *)
    echo "Unsupported architecture: ${ARCH}. Supported: arm64, x64." >&2
    exit 1
    ;;
esac

mkdir -p "${INSTALL_DIR}"
TMP_DIR="$(mktemp -d)"
trap 'rm -rf "${TMP_DIR}"' EXIT

ARCHIVE_PATH="${TMP_DIR}/${FILE}"
echo "Downloading ${FILE}..."
curl -fL "${BASE_URL}/${FILE}" -o "${ARCHIVE_PATH}"

echo "Extracting..."
tar -xzf "${ARCHIVE_PATH}" -C "${TMP_DIR}"

BIN_PATH="$(find "${TMP_DIR}" -type f -name "iv*" | head -n 1)"
if [[ -z "${BIN_PATH}" ]]; then
  echo "Unable to find Ivory binary in the archive." >&2
  exit 1
fi

DEST="${INSTALL_DIR}/iv"
cp "${BIN_PATH}" "${DEST}"
chmod +x "${DEST}"

if [[ ":${PATH}:" != *":${INSTALL_DIR}:"* ]]; then
  export PATH="${INSTALL_DIR}:${PATH}"
  PROFILE_FILE="${HOME}/.zprofile"
  [ -f "${HOME}/.zshrc" ] && PROFILE_FILE="${HOME}/.zshrc"
  if ! grep -F "${INSTALL_DIR}" "${PROFILE_FILE}" >/dev/null 2>&1; then
    printf '\n# Added by Ivory installer\nexport PATH="%s:$PATH"\n' "${INSTALL_DIR}" >> "${PROFILE_FILE}"
  fi
fi

echo "Ivory installed to ${DEST}"
echo "Restart your shell or run: export PATH=\"${INSTALL_DIR}:\$PATH\""
