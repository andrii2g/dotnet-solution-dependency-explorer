#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd -P)"
project_path="$repo_root/src/DependencyExplorer/DependencyExplorer.csproj"
tool_output="$repo_root/artifacts/validation/tool"
validation_root="$repo_root/artifacts/validation/runs"
examples_root="$repo_root/docs/examples"

normalize_file() {
  local input_path="$1"
  python3 - "$input_path" "$repo_root" <<'PY'
from pathlib import Path
import re
import sys

input_path = Path(sys.argv[1])
repo_root = sys.argv[2]
text = input_path.read_text(encoding="utf-8")
text = text.replace("\r\n", "\n").replace("\r", "\n")
text = text.replace(repo_root.replace("\\", "/"), ".")
text = text.replace(repo_root, ".")
lines = [line.rstrip() for line in text.split("\n")]
text = "\n".join(lines)
text = re.sub(r"\n{3,}", "\n\n", text)
text = text.strip() + "\n"
sys.stdout.write(text)
PY
}

compare_normalized_file() {
  local expected_path="$1"
  local actual_path="$2"

  if [[ ! -f "$expected_path" ]]; then
    echo "Missing snapshot file: $expected_path" >&2
    exit 1
  fi

  if [[ ! -f "$actual_path" ]]; then
    echo "Missing generated file: $actual_path" >&2
    exit 1
  fi

  local expected_tmp actual_tmp
  expected_tmp="$(mktemp)"
  actual_tmp="$(mktemp)"
  trap 'rm -f "$expected_tmp" "$actual_tmp"' RETURN

  normalize_file "$expected_path" > "$expected_tmp"
  normalize_file "$actual_path" > "$actual_tmp"

  if ! diff -u "$expected_tmp" "$actual_tmp"; then
    echo "Snapshot comparison failed for $actual_path" >&2
    exit 1
  fi

  rm -f "$expected_tmp" "$actual_tmp"
  trap - RETURN
}

invoke_analyzer() {
  local tool_dll="$1"
  local solution_path="$2"
  local output_directory="$3"

  rm -rf "$output_directory"
  mkdir -p "$output_directory"
  dotnet "$tool_dll" analyze \
    --solution "$solution_path" \
    --output "$output_directory" \
    --level all \
    --verbose
}

prepare_fixture_solution() {
  local solution_root="$1"

  find "$solution_root" -type d \( -name bin -o -name obj \) -prune -exec rm -rf {} +
  while IFS= read -r project_path; do
    dotnet restore "$project_path"
  done < <(find "$solution_root" -name '*.csproj' | sort)
}

echo "Publishing analyzer..."
rm -rf "$repo_root/src/DependencyExplorer/bin" "$repo_root/src/DependencyExplorer/obj" "$tool_output" "$validation_root"
dotnet restore "$project_path"
dotnet publish "$project_path" -c Debug -o "$tool_output" /p:UseAppHost=false

tool_dll="$tool_output/DependencyExplorer.dll"
layered_solution="$repo_root/samples/Fixtures/LayeredSample/LayeredSample.slnx"
mixed_solution="$repo_root/samples/Fixtures/MixedLegacySample/MixedLegacySample.slnx"
layered_root="$repo_root/samples/Fixtures/LayeredSample"
mixed_root="$repo_root/samples/Fixtures/MixedLegacySample"
layered_output="$validation_root/LayeredSample"
mixed_output="$validation_root/MixedLegacySample"

prepare_fixture_solution "$layered_root"
prepare_fixture_solution "$mixed_root"

invoke_analyzer "$tool_dll" "$layered_solution" "$layered_output"
invoke_analyzer "$tool_dll" "$mixed_solution" "$mixed_output"

compare_normalized_file "$examples_root/LayeredSample/graph-projects.mmd" "$layered_output/graph-projects.mmd"
compare_normalized_file "$examples_root/LayeredSample/graph-namespaces.mmd" "$layered_output/graph-namespaces.mmd"
compare_normalized_file "$examples_root/LayeredSample/summary.md" "$layered_output/summary.md"
compare_normalized_file "$examples_root/MixedLegacySample/violations.md" "$mixed_output/violations.md"

echo "Fixture validation passed."
