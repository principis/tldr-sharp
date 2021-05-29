FROM docker.io/library/fedora:34

# Copying all contents of rpmbuild repo inside container
WORKDIR /app

# Installing tools needed for rpmbuild
RUN dnf --setopt=tsflags=nodocs -y install rpm-build rpmdevtools coreutils mono-devel nuget

COPY entrypoint.sh /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
