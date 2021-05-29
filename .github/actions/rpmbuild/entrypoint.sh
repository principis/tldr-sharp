#!/bin/sh -l
set -ex

SPEC_PATH=$1
SOURCES_PATH=$2
SPEC_FILE=$(basename "$SPEC_PATH")

# Create rpmbuild dir tree
rpmdev-setuptree
cp "/github/workspace/${SPEC_PATH}" /github/home/rpmbuild/SPECS/
cp -a "/github/workspace/${SOURCES_PATH}/." /github/home/rpmbuild/SOURCES/

rpmbuild -bb "/github/home/rpmbuild/SPECS/${SPEC_FILE}"

mkdir -p rpmbuild/RPMS
cp -r /github/home/rpmbuild/RPMS/. rpmbuild/RPMS/

rpm_dir_path="rpmbuild/RPMS/"
echo "::set-output name=rpm_dir_path::${rpm_dir_path}"