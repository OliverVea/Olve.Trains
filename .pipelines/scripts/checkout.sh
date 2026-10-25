#!/bin/sh
# The GitHub tarball holds only LFS pointers, so clone and pull the LFS files.
set -e
export DEBIAN_FRONTEND=noninteractive
apt-get update -qq
apt-get install -y -qq --no-install-recommends git git-lfs ca-certificates >/dev/null

git lfs install --skip-repo
git init -q "$DEST"
cd "$DEST"
git remote add origin "https://x-access-token:$GITHUB_TOKEN@github.com/$REPO.git"
git fetch -q --depth 1 origin "$SHA"
git checkout -q FETCH_HEAD
git lfs pull
rm -rf .git
