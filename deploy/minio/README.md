# Olve.Trains release-artifact MinIO

A private, in-cluster, single-instance [MinIO](https://min.io) object store that holds the
distribution bucket(s) for On Track To Grow release archives (and CI screenshot-diff
exports). It replaces the AWS S3 dist bucket.

## Model

"Per-pipeline storage" — each app that needs object storage runs **its own** MinIO. This
one is owned and deployed by the **Olve.Trains** pipeline itself (the `bootstrap-minio`
processing step in `.pipelines/config.yaml`), which `scp`s these manifests to `bulwark-m2`
and `kubectl apply`s them over SSH (same SSH-to-host pattern the homelab apps use to
deploy). It is **not** the pipeline controller's internal bundle MinIO, and **not** the
retired shared `minio.ovea.pro`.

- **Namespace:** `olve-runners` (beside the runner Jobs that consume it).
- **Endpoint (in-cluster only):** `http://olve-trains-minio.olve-runners.svc.cluster.local:9000`
  Reachable cross-namespace from `olve-runners-beta` Jobs too.
- **Exposure:** none. ClusterIP Service, S3 API port 9000 only. Public exposure
  (Cloudflare-fronted, private origin) is a deferred *Later* item, not part of this.
- **Storage:** 50Gi on the `bulk` StorageClass (the 8 Ti HDD; reclaim `Retain`).
- **Buckets:** `olve-trains-beta` and `olve-trains-prod`, created idempotently by
  `publish-minio.sh` — not by these manifests.

## Prerequisite: root-credentials Secret (out-of-band)

Secrets are **never committed**. Before the bootstrap step can apply the Deployment,
create the root-credentials Secret in `olve-runners` once, by hand on the cluster:

```bash
kubectl create secret generic olve-trains-minio-credentials \
  -n olve-runners \
  --from-literal=root-user="$(openssl rand -hex 16)" \
  --from-literal=root-password="$(openssl rand -hex 32)"
```

The Deployment reads `root-user` / `root-password` from this Secret into
`MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD`.

## Prerequisite: pipeline secrets

The Olve.Trains pipeline secret (`olve-pipeline-<id>`) must also carry:

- `SSH_PRIVATE_KEY` — key for `oliver@bulwark-m2`, so the bootstrap step can `kubectl apply`.
- `MINIO__Key` / `MINIO__Secret` — the access key the publish/test steps use to write to
  MinIO (set these to the MinIO root user/password above, or to a dedicated MinIO access
  key created against this instance).

## Apply / validate manually (do this before wiring the bootstrap step into master)

Every push to `master` is a live deploy, so prove the manifests out-of-band first:

```bash
# From a machine with cluster access (or over SSH to bulwark-m2):
kubectl apply --dry-run=server -f deploy/minio/minio.yaml   # server-side validation
kubectl apply -f deploy/minio/minio.yaml                    # real apply
kubectl -n olve-runners rollout status deploy/olve-trains-minio --timeout=120s
```

Then confirm an S3 client can create a bucket against the in-cluster endpoint before
relying on it from the pipeline.
