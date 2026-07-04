#!/bin/sh
# Processing step: publish the release archives to the self-hosted MinIO dist buckets
# (olve-trains-beta then olve-trains-prod), replacing the AWS S3 dist bucket.
#
# awscli points at the in-cluster MinIO endpoint with path-style addressing (bucket in the
# path, not the hostname) and region us-east-1. Buckets are created if absent (idempotent)
# WITHOUT a LocationConstraint — MinIO/us-east-1 rejects the AWS-only create-bucket-config.
# Each archive lands under <prefix>/<version>/ and is mirrored to <prefix>/latest/.
#
# The endpoint is cluster-internal (no external route yet — public exposure is a Later
# item), so no presigned URLs are logged: they'd carry the private host and not resolve.
set -e

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y --no-install-recommends awscli ca-certificates

export AWS_ACCESS_KEY_ID="$MINIO__Key"
export AWS_SECRET_ACCESS_KEY="$MINIO__Secret"
export AWS_DEFAULT_REGION="$MINIO_REGION"
# MinIO requires path-style addressing.
aws configure set default.s3.addressing_style path

ensure_bucket() {
  b=$1
  if ! aws --endpoint-url "$MINIO_ENDPOINT" s3api head-bucket --bucket "$b" 2>/dev/null; then
    echo "creating bucket $b"
    aws --endpoint-url "$MINIO_ENDPOINT" s3api create-bucket --bucket "$b"
  fi
}

VERSION=$(cat "$(ls /input/*/version.txt | head -1)")

# Publish to beta first, then prod (env split is by bucket, one MinIO instance).
for BUCKET in "$MINIO_BETA_BUCKET" "$MINIO_PROD_BUCKET"; do
  ensure_bucket "$BUCKET"
  for f in /input/*/olve-trains-*-linux-x64.tar.gz /input/*/olve-trains-*-win-x64.zip; do
    [ -f "$f" ] || continue
    base=$(basename "$f")
    versioned="s3://$BUCKET/$MINIO_PREFIX/$VERSION/$base"
    latest="s3://$BUCKET/$MINIO_PREFIX/latest/$base"

    aws --endpoint-url "$MINIO_ENDPOINT" s3 cp "$f" "$versioned"
    aws --endpoint-url "$MINIO_ENDPOINT" s3 cp "$f" "$latest"
    echo "uploaded: $versioned"
  done
done
