#!/bin/sh
# Processing step (runs FIRST, before test/publish): ensure the Olve.Trains release-artifact
# MinIO exists in the cluster. Keeps the pipeline the full source of truth for the deploy —
# the manifest lives in this repo (deploy/minio/minio.yaml), applied here on every push.
#
# Idempotent: `kubectl apply` over SSH to bulwark-m2 (the same SSH-to-host pattern the
# homelab apps use to deploy). A no-op push re-applies with no effect. kubectl runs on the
# host against its admin kubeconfig — this container only needs ssh/scp + the repo fetch.
#
# Prereqs (out-of-band, NOT in repo): the `olve-trains-minio-credentials` Secret in
# olve-runners, and SSH_PRIVATE_KEY in the pipeline secret. See deploy/minio/README.md.
set -e

# Shared helpers (olve_ssh_host + olve_fetch_repo). Fetch-to-file then source — busybox ash
# has no process substitution. Swap `main` for a tag/SHA to pin.
mkdir -p /tmp
wget --no-check-certificate -qO /tmp/olve-lib.sh \
  https://raw.githubusercontent.com/OliverVea/Olve.Pipelines/main/.pipelines/scripts/olve-lib.sh
. /tmp/olve-lib.sh

HOST=oliver@bulwark-m2
NS=olve-runners
RELEASE=olve-trains-minio

# The manifest lives in the repo, but a processing step only receives the /input bundle, not
# the source — fetch the repo tarball (private, so via the API with $GITHUB_TOKEN) for it.
olve_fetch_repo "$REPO" "$BRANCH" /tmp/src

olve_ssh_host bulwark-m2

scp -o StrictHostKeyChecking=no /tmp/src/deploy/minio/minio.yaml "$HOST:/tmp/$RELEASE.yaml"
ssh -o StrictHostKeyChecking=no "$HOST" \
  "kubectl apply -f /tmp/$RELEASE.yaml \
     && kubectl -n $NS rollout status deploy/$RELEASE --timeout=120s \
     && rm -f /tmp/$RELEASE.yaml"
