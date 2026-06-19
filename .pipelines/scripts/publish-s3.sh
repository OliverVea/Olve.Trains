#!/bin/sh
# Processing step: upload the release archives to the shareable distribution bucket.
#
# Uploads each platform archive under <prefix>/<version>/ and mirrors it to <prefix>/latest/,
# so there is both an immutable versioned URL and a stable "latest" link. Objects are made
# public-read by default (durable shareable links); a 7-day presigned URL is also logged for
# convenience. Set S3_DIST_ENDPOINT for an S3-compatible (non-AWS) bucket.
set -e

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y --no-install-recommends awscli ca-certificates

export AWS_ACCESS_KEY_ID="$S3_DIST_KEY"
export AWS_SECRET_ACCESS_KEY="$S3_DIST_SECRET"
[ -n "$S3_DIST_REGION" ] && export AWS_DEFAULT_REGION="$S3_DIST_REGION"

EP=""
[ -n "$S3_DIST_ENDPOINT" ] && EP="--endpoint-url $S3_DIST_ENDPOINT"
ACL=""
[ -n "$S3_DIST_ACL" ] && ACL="--acl $S3_DIST_ACL"

VERSION=$(cat "$(ls /input/*/version.txt | head -1)")

for f in /input/*/olve-trains-*-linux-x64.tar.gz /input/*/olve-trains-*-win-x64.zip; do
  [ -f "$f" ] || continue
  base=$(basename "$f")
  versioned="s3://$S3_DIST_BUCKET/$S3_DIST_PREFIX/$VERSION/$base"
  latest="s3://$S3_DIST_BUCKET/$S3_DIST_PREFIX/latest/$base"

  aws s3 cp "$f" "$versioned" $ACL $EP
  aws s3 cp "$f" "$latest"    $ACL $EP

  if [ -n "$S3_DIST_PUBLIC_BASE" ]; then
    echo "public: $S3_DIST_PUBLIC_BASE/$S3_DIST_PREFIX/$VERSION/$base"
    echo "latest: $S3_DIST_PUBLIC_BASE/$S3_DIST_PREFIX/latest/$base"
  else
    echo "uploaded: $versioned"
  fi
  echo "presigned (7d): $(aws s3 presign "$versioned" --expires-in 604800 $EP)"
done
