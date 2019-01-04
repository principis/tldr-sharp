#! /bin/bash

set -e

if ([ ! -z "$TRAVIS_TAG" ]) && 
      [ "$TRAVIS_PULL_REQUEST" == "false" ]; then

    cd tldr-sharp/bin/Release
    zip -r "../$TRAVIS_TAG.zip" *
    rm Mono.Data.Sqlite.dll || true # not necessary on linux
    tar czf "../$TRAVIS_TAG.tar.gz" * 

    sed -i "s/VERSION_PLACEHOLDER/$TRAVIS_TAG/g" ../../../scripts/linux_install.sh

    # Build debian package

    tempdir=$(mktemp -d 2>/dev/null || mktemp -d -t tmp)
    mkdir -p "$tempdir/usr/lib/tldr-sharp"
    cp * "$tempdir/usr/lib/tldr-sharp"
    chmod 755 "$tempdir/usr/lib/tldr-sharp/"*

    mkdir -p "$tempdir/usr/bin"
    install -Dm755 "../../../scripts/debian/tldr-sharp" "$tempdir/usr/bin/tldr-sharp"

    mkdir "$tempdir/DEBIAN"
    install -Dm644 "../../../scripts/debian/control" "$tempdir/DEBIAN/control"
    install -Dm755 "../../../scripts/debian/postinst" "$tempdir/DEBIAN/postinst"
    install -Dm755 "../../../scripts/debian/prerm" "$tempdir/DEBIAN/prerm"

    sed -i "s/VERSION_PLACEHOLDER/${TRAVIS_TAG#v}/g" "$tempdir/DEBIAN/control"

    fakeroot dpkg-deb --build "$tempdir" "../tldr-sharp_${TRAVIS_TAG}_amd64.deb"

fi


