#!/usr/bin/env bash
set -euo pipefail

INSTALL_DIR="${HOME}/.ivory/bin"

remove_path_line() {
  local file="$1"
  [ -f "${file}" ] || return 0
  local tmp
  tmp="$(mktemp)"
  # Strip lines we added that contain the install dir or our marker comment.
  grep -vF "${INSTALL_DIR}" "${file}" | grep -vF "# Added by Ivory installer" > "${tmp}" || true
  mv "${tmp}" "${file}"
}

echo "Removing Ivory binary from ${INSTALL_DIR}..."
rm -f "${INSTALL_DIR}/iv"

# Clean up empty directory tree if nothing else is there.
if [ -d "${INSTALL_DIR}" ] && [ -z "$(ls -A "${INSTALL_DIR}")" ]; then
  rmdir "${INSTALL_DIR}"
fi
if [ -d "${HOME}/.ivory" ] && [ -z "$(ls -A "${HOME}/.ivory")" ]; then
  rmdir "${HOME}/.ivory"
fi

for profile in "${HOME}/.profile" "${HOME}/.zprofile" "${HOME}/.zshrc"; do
  remove_path_line "${profile}"
done

echo "Ivory uninstalled. Restart your shell to ensure PATH changes are applied."
