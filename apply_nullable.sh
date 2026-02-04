#!/bin/bash
find . -name '*.cs' -type f | while read -r f; do
  if ! grep -q '#nullable enable' "$f"; then
    sed -i '1i#nullable enable' "$f"
  fi
done
