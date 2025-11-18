#!/usr/bin/env bash

# Installs the required .NET SDK, restores dependencies, and installs Playwright browsers
# for tests/NarratoriaClient.PlaywrightTests. Suitable for CI images/containers.
# Linux system deps (Debian/Ubuntu): libatk1.0-0 libatk-bridge2.0-0 libdrm2 libgbm1 libgtk-3-0 libnspr4 libnss3 libxkbcommon0 libxcomposite1 libxdamage1 libxfixes3 libxrandr2 libxshmfence1

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
DOTNET_CHANNEL="${DOTNET_CHANNEL:-9.0}"
DOTNET_INSTALL_DIR="${DOTNET_INSTALL_DIR:-${REPO_ROOT}/.dotnet}"
CONFIGURATION="${CONFIGURATION:-Debug}"
TEST_PROJECT="${REPO_ROOT}/tests/NarratoriaClient.PlaywrightTests/NarratoriaClient.PlaywrightTests.csproj"
PLAYWRIGHT_DIR="${REPO_ROOT}/tests/NarratoriaClient.PlaywrightTests/bin/${CONFIGURATION}/net9.0"
PLAYWRIGHT_DLL="${PLAYWRIGHT_DIR}/Microsoft.Playwright.dll"
PLAYWRIGHT_RUNTIMECONFIG="${PLAYWRIGHT_DIR}/NarratoriaClient.PlaywrightTests.runtimeconfig.json"
PLAYWRIGHT_BROWSERS_PATH="${PLAYWRIGHT_BROWSERS_PATH:-${REPO_ROOT}/.playwright-browsers}"

log() {
  echo "[playwright-setup] $*"
}

ensure_dotnet() {
  if command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks | grep -E "^${DOTNET_CHANNEL}\." >/dev/null 2>&1; then
    DOTNET_CMD="dotnet"
    return
  fi

  log "Installing .NET SDK channel ${DOTNET_CHANNEL} into ${DOTNET_INSTALL_DIR}..."
  mkdir -p "${DOTNET_INSTALL_DIR}"
  curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel "${DOTNET_CHANNEL}" --quality ga --install-dir "${DOTNET_INSTALL_DIR}"

  DOTNET_CMD="${DOTNET_INSTALL_DIR}/dotnet"
  export DOTNET_ROOT="${DOTNET_INSTALL_DIR}"
  export PATH="${DOTNET_ROOT}:${PATH}"
}

main() {
  ensure_dotnet

  if [[ -z "${DOTNET_CMD:-}" ]]; then
    log "Failed to locate or install a .NET SDK for channel ${DOTNET_CHANNEL}."
    exit 1
  fi

  log "Using dotnet from $(command -v "${DOTNET_CMD}") ($( "${DOTNET_CMD}" --version))"
  export PLAYWRIGHT_BROWSERS_PATH
  log "Playwright browser cache: ${PLAYWRIGHT_BROWSERS_PATH}"

  log "Restoring solution dependencies..."
  "${DOTNET_CMD}" restore "${REPO_ROOT}/narratoria.sln"

  log "Building Playwright test project (${CONFIGURATION})..."
  "${DOTNET_CMD}" build "${TEST_PROJECT}" -c "${CONFIGURATION}"

  if [[ ! -f "${PLAYWRIGHT_DLL}" || ! -f "${PLAYWRIGHT_RUNTIMECONFIG}" ]]; then
    log "Expected Playwright CLI artifacts not found under ${PLAYWRIGHT_DIR}"
    exit 1
  fi

  export PLAYWRIGHT_DRIVER_SEARCH_PATH="${PLAYWRIGHT_DIR}"
  install_args=(install)
  if [[ "${INSTALL_PLAYWRIGHT_DEPS:-false}" == "true" && "$(uname -s)" == "Linux" ]]; then
    log "Installing Playwright browsers with Linux system dependencies (--with-deps). May require sudo on some images."
    install_args+=(--with-deps)
  else
    log "Installing Playwright browsers (headless). Set INSTALL_PLAYWRIGHT_DEPS=true to also install Linux system packages."
  fi

  "${DOTNET_CMD}" exec --runtimeconfig "${PLAYWRIGHT_RUNTIMECONFIG}" "${PLAYWRIGHT_DLL}" "${install_args[@]}"

  log "Playwright setup complete. Run tests with:"
  log "  PLAYWRIGHT_BROWSERS_PATH=${PLAYWRIGHT_BROWSERS_PATH} DOTNET_ENVIRONMENT=Testing ${DOTNET_CMD} test ${TEST_PROJECT} -c ${CONFIGURATION}"
}

main "$@"
