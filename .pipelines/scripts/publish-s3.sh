#!/bin/sh
# Processing step: upload the release archives to a pipeline-owned distribution bucket.
#
# Reuses the asset-pipeline AWS identity (S3__Key/S3__Secret) — no separate credential.
# The bucket is created here if absent (idempotent), so there is no manual prerequisite.
# Each archive lands under <prefix>/<version>/ and is mirrored to <prefix>/latest/, giving
# an immutable versioned key plus a stable "latest" key. A 7-day presigned URL is logged
# per artifact (the default share mechanism; AWS buckets are private by default).
set -e

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y --no-install-recommends awscli ca-certificates

export AWS_ACCESS_KEY_ID="$S3__Key"
export AWS_SECRET_ACCESS_KEY="$S3__Secret"
export AWS_DEFAULT_REGION="$S3_DIST_REGION"

ACL=""
[ -n "$S3_DIST_ACL" ] && ACL="--acl $S3_DIST_ACL"

# Create the dist bucket if it does not exist yet (needs CreateBucket on the identity).
if ! aws s3api head-bucket --bucket "$S3_DIST_BUCKET" 2>/dev/null; then
  echo "creating bucket $S3_DIST_BUCKET in $S3_DIST_REGION"
  aws s3api create-bucket --bucket "$S3_DIST_BUCKET" \
    --region "$S3_DIST_REGION" \
    --create-bucket-configuration "LocationConstraint=$S3_DIST_REGION"
fi

VERSION=$(cat "$(ls /input/*/version.txt | head -1)")

for f in /input/*/olve-trains-*-linux-x64.tar.gz /input/*/olve-trains-*-win-x64.zip; do
  [ -f "$f" ] || continue
  base=$(basename "$f")
  versioned="s3://$S3_DIST_BUCKET/$S3_DIST_PREFIX/$VERSION/$base"
  latest="s3://$S3_DIST_BUCKET/$S3_DIST_PREFIX/latest/$base"

  aws s3 cp "$f" "$versioned" $ACL
  aws s3 cp "$f" "$latest"    $ACL

  if [ -n "$S3_DIST_PUBLIC_BASE" ]; then
    echo "public: $S3_DIST_PUBLIC_BASE/$S3_DIST_PREFIX/$VERSION/$base"
    echo "latest: $S3_DIST_PUBLIC_BASE/$S3_DIST_PREFIX/latest/$base"
  else
    echo "uploaded: $versioned"
  fi
  echo "presigned (7d): $(aws s3 presign "$versioned" --expires-in 604800)"
done
