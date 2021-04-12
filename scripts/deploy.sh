#! /bin/bash

set -e

SCRIPT=$(realpath "$0")
SCRIPTPATH=$(dirname "$SCRIPT")

if [ ! -z "$TRAVIS_TAG" ] &&
	[ "$TRAVIS_PULL_REQUEST" == "false" ]; then

	create_archives() {

		cd "$TARGET"

		# Windows archives
		cp "${SCRIPTPATH}/windows/sqlite3${PLATFORM}.dll" sqlite3.dll
		zip -r "../tldr-sharp_windows${PLATFORM}.zip" ./*

		# Linux archives
		rm Mono.Data.Sqlite.dll sqlite3.dll || true # not necessary on linux
		tar czf "../tldr-sharp_linux${PLATFORM}.tar.gz" ./*

		cd ..
	}

	build_deb() {
		local tempdir

		tempdir=$(mktemp -d 2>/dev/null || mktemp -d -t tmp)

		mkdir -p "$tempdir/usr/lib/tldr-sharp"
		cp -r "$TARGET"/* "$tempdir/usr/lib/tldr-sharp"
		chmod 755 "$tempdir/usr/lib/tldr-sharp/"*

		mkdir -p "$tempdir/usr/bin"
		install -Dm755 "${SCRIPTPATH}/debian/tldr-sharp" "$tempdir/usr/bin/tldr-sharp"

		mkdir "$tempdir/DEBIAN"
		install -Dm644 "${SCRIPTPATH}/debian/control" "$tempdir/DEBIAN/control"
		install -Dm755 "${SCRIPTPATH}/debian/postinst" "$tempdir/DEBIAN/postinst"
		install -Dm755 "${SCRIPTPATH}/debian/prerm" "$tempdir/DEBIAN/prerm"

		sed -i "s/VERSION_PLACEHOLDER/${TRAVIS_TAG#v}/g" "$tempdir/DEBIAN/control"

		fakeroot dpkg-deb --build "$tempdir" "tldr-sharp${PLATFORM}.deb"
	}

	cd tldr-sharp/bin

	TARGET="Release"
	PLATFORM="_x64"
	create_archives
	build_deb

	TARGET="Release32"
	PLATFORM=""
	create_archives
	build_deb

fi
