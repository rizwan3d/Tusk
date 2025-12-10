#!/usr/bin/env bash
set -euo pipefail

BASE_URL="https://github.com/rizwan3d/Tusk/releases/latest/download"
INSTALL_DIR="${HOME}/.tusk/bin"

if ! command -v curl >/dev/null 2>&1; then
  echo "curl is required to download Tusk." >&2
  exit 1
fi

if ! command -v tar >/dev/null 2>&1; then
  echo "tar is required to extract the Tusk archive." >&2
  exit 1
fi

ARCH="$(uname -m)"
case "${ARCH}" in
  x86_64|amd64) FILE="tusk-linux-x64.tar.gz" ;;
  *)
    echo "Unsupported architecture: ${ARCH}. Only x64 is supported on Linux." >&2
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

BIN_PATH="$(find "${TMP_DIR}" -type f -name "tusk*" | head -n 1)"
if [[ -z "${BIN_PATH}" ]]; then
  echo "Unable to find Tusk binary in the archive." >&2
  exit 1
fi

DEST="${INSTALL_DIR}/tusk"
cp "${BIN_PATH}" "${DEST}"
chmod +x "${DEST}"

if [[ ":${PATH}:" != *":${INSTALL_DIR}:"* ]]; then
  export PATH="${INSTALL_DIR}:${PATH}"
  PROFILE_FILE="${HOME}/.profile"
  if ! grep -F "${INSTALL_DIR}" "${PROFILE_FILE}" >/dev/null 2>&1; then
    printf '\n# Added by Tusk installer\nexport PATH="%s:$PATH"\n' "${INSTALL_DIR}" >> "${PROFILE_FILE}"
  fi
fi

echo "Tusk installed to ${DEST}"
echo "Restart your shell or run: export PATH=\"${INSTALL_DIR}:\$PATH\""
