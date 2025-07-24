#!/usr/bin/env bash
set -euo pipefail

# ----- params -----
CONFIG=${1:-debug}
if [[ "$CONFIG" != "debug" && "$CONFIG" != "release" ]]; then
  echo "Usage: $0 [debug|release] [framework]"
  exit 1
fi
FRAMEWORK=${2:-net9.0}

# ----- roots -----
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"    # .../CustomEngine/Scripts
SOLUTION_ROOT="$(dirname "$SCRIPT_DIR")"                      # .../CustomEngine

BGFX_PATH="$SOLUTION_ROOT/Submodules/bgfx"
if [[ ! -d "$BGFX_PATH" ]]; then
  echo "ERROR: bgfx folder not found at '$BGFX_PATH'."
  exit 1
fi

# ----- fixed paths -----
SC="$BGFX_PATH/.build/osx-arm64/bin/shadercRelease"
SHDR="$SOLUTION_ROOT/Engine.Assets/Materials"
OUTPUT_BASE="$SOLUTION_ROOT/Engine.Editor/bin/$CONFIG/$FRAMEWORK/Compiled/Shaders"

# ----- common params -----
common_params=(
  --platform osx
  -p metal
  -i "$BGFX_PATH/src"
  -i "$BGFX_PATH/examples/common"
  --varyingdef "$SHDR/varying.def.sc"
)

# ----- helpers -----
CYAN="\033[36m"; GREEN="\033[32m"; RED="\033[31m"; YELLOW="\033[33m"; RESET="\033[0m"

section() {
  printf "\n${CYAN}%s${RESET}\n" "$1"
  printf "${CYAN}%${#1}s${RESET}\n" "" | tr " " "-"
}

write_result() {
  local src="$1" dst="$2" ok="$3" pad="$4"
  local flag color
  if [[ "$ok" -eq 0 ]]; then
    flag="[OK]" ; color=$GREEN
  else
    flag="[!!]" ; color=$RED
  fi
  printf "${color}%-4s${RESET} %-${pad}s -> %s\n" "$flag" "$src" "$dst"
}

# compile_shaders: prints logs, sets COUNT to number of files processed
compile_shaders() {
  local filter="$1" type="$2"
  section "-- Compiling $type shaders"

  # find longest relative path for padding
  local max=0
  while IFS= read -r -d $'\0' file; do
    rel="${file#$SHDR/}"
    (( ${#rel} > max )) && max=${#rel}
  done < <(find "$SHDR" -type f -name "$filter" -print0)

  local count=0
  while IFS= read -r -d $'\0' file; do
    rel="${file#$SHDR/}"
    out="$OUTPUT_BASE/${rel%.glsl}.bin"
    mkdir -p "$(dirname "$out")"

    stderr="$("$SC" -f "$file" -o "$out" --type "$type" "${common_params[@]}" 2>&1)"
    code=$?
    write_result "$rel" "${out#$OUTPUT_BASE/}" $code $((max+2))
    if [[ $code -ne 0 ]]; then
      printf "${YELLOW}%s${RESET}\n" "$stderr"
    fi

    ((count++))
  done < <(find "$SHDR" -type f -name "$filter" -print0)

  COUNT=$count
}

# ----- main -----
start_time=$(date +%s)

compile_shaders '*.vert.glsl' vertex
vcount=$COUNT

compile_shaders '*.frag.glsl' fragment
fcount=$COUNT

end_time=$(date +%s)
elapsed=$((end_time - start_time))
elapsed_fmt=$(printf "%.2f" "$elapsed")

printf "\n${CYAN}Done: %s vertex + %s fragment shaders in %s s${RESET}\n" \
       "$vcount" "$fcount" "$elapsed_fmt"
