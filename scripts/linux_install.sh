#!/bin/bash

echo '[INFO] Installing tldr-sharp'

tldrLib='/usr/local/lib'
tldrBin='/usr/local/bin'

cd /tmp
if [ -d "/tmp/tldr" ] || [ -f "/tmp/tldr" ]; then
    rm -rf "/tmp/tldr"
fi

# Download release

mkdir tldr
wget -q https://github.com/principis/tldr-sharp/releases/download/VERSION_PLACEHOLDER/FILE_PLACEHOLDER.tar.gz
retval=$?

if [ $retval != 0 ]; then
    echo '[ERROR] Failed to download release!'
    exit 1
fi

# Extract release

tar xzf FILE_PLACEHOLDER.tar.gz -C tldr
rm FILE_PLACEHOLDER.tar.gz
if [ -d "$tldrLib/tldr" ] || [ -f "$tldrLib/tldr" ]; then
    sudo rm -rf "$tldrLib/tldr" 2> /dev/null
fi

# Move release

sudo mv tldr "$tldrLib"
retval=$?

if [ $retval != 0 ]; then
    echo '[ERROR] Failed to move to install location!'
    exit 1
fi

rm -rf "/tmp/tldr"

# Download executable

wget -q https://raw.githubusercontent.com/principis/tldr-sharp/master/tldr
retval=$?

if [ $retval != 0 ]; then
    echo '[ERROR] Failed to download tldr executable!'
    exit 1
fi

# Install executable

sudo install -m755 tldr "$tldrBin"
retval=$?

if [ $retval != 0 ]; then
    echo '[ERROR] Failed to move to install location!'
    exit 1
fi

# Delete tmp
rm tldr

echo 'Finished'
