#! /bin/bash
if ([ ! -z "$TRAVIS_TAG" ]) && 
      [ "$TRAVIS_PULL_REQUEST" == "false" ]; then

    cd tldr-sharp/bin/Release
    tar czf "../$TRAVIS_TAG.tar.gz" *
    zip -r "../$TRAVIS_TAG.zip" *

    sed -i "s/VERSION_PLACEHOLDER/$TRAVIS_TAG/g" ../../../scripts/linux_install.sh
fi


