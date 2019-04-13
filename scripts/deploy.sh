#! /bin/bash

set -e

if ([ ! -z "$TRAVIS_TAG" ]) && 
      [ "$TRAVIS_PULL_REQUEST" == "false" ]; then
 
    create_archives() {
		
		cd $TARGET
	
		# Windows archives
		zip -r "../tldr-sharp_${TRAVIS_TAG#v}_windows${PLATFORM}.zip" *

		# Linux archives
		rm $TARGET/Mono.Data.Sqlite.dll || true # not necessary on linux
		tar czf "../tldr-sharp_${TRAVIS_TAG#v}_linux${PLATFORM}.tar.gz" *

		cd ..

		# Linux install scripts
			sed -e "s/VERSION_PLACEHOLDER/$TRAVIS_TAG/" -e "s/FILE_PLACEHOLDER/tldr-sharp_${TRAVIS_TAG#v}_linux${PLATFORM}/" ../../scripts/linux_install.sh > tldr-sharp_linux${PLATFORM}.sh
    }

    build_deb() {
	    local tempdir

    	tempdir=$(mktemp -d 2>/dev/null || mktemp -d -t tmp)

    	mkdir -p "$tempdir/usr/lib/tldr-sharp"
	    cp $TARGET/* "$tempdir/usr/lib/tldr-sharp"
    	chmod 755 "$tempdir/usr/lib/tldr-sharp/"*

    	mkdir -p "$tempdir/usr/bin"
    	install -Dm755 "../../scripts/debian/tldr-sharp" "$tempdir/usr/bin/tldr-sharp"

    	mkdir "$tempdir/DEBIAN"
    	install -Dm644 "../../scripts/debian/control" "$tempdir/DEBIAN/control"
    	install -Dm755 "../../scripts/debian/postinst" "$tempdir/DEBIAN/postinst"
    	install -Dm755 "../../scripts/debian/prerm" "$tempdir/DEBIAN/prerm"

    	sed -i "s/VERSION_PLACEHOLDER/${TRAVIS_TAG#v}/g" "$tempdir/DEBIAN/control"

    	fakeroot dpkg-deb --build "$tempdir" "tldr-sharp_${TRAVIS_TAG#v}${PLATFORM}.deb"
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

