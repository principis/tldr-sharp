#!/bin/bash

# SPDX-FileCopyrightText: None
# SPDX-License-Identifier: CC0-1.0

if [[ $EUID -ne 0 ]]; then
   echo "[ERROR] This script must be run as root"
   exit 1
fi

echo '[INFO] Installing tldr-sharp'

tldrLib='/usr/local/lib'
tldrBin='/usr/local/bin'

cd /tmp
if [ -d "/tmp/tldr" ] || [ -f "/tmp/tldr" ]; then
    rm -rf "/tmp/tldr"
fi

# Download release

mkdir tldr
wget -q "https://github.com/principis/tldr-sharp/releases/latest/download/tldr-sharp_linux-x64.tar.gz" -O tldr-sharp.tar.gz
retval=$?

if [ $retval != 0 ]; then
    echo '[ERROR] Failed to download release!'
    exit 1
fi

# Extract release

tar xzf tldr-sharp.tar.gz -C tldr
rm tldr-sharp.tar.gz
if [ -d "$tldrLib/tldr" ] || [ -f "$tldrLib/tldr" ]; then
    rm -rf "$tldrLib/tldr" 2> /dev/null
fi

# Move release

install -Dm 755 tldr/tldr-sharp "${tldrLib}/tldr"
find tldr -type f -name "*.dll" -o -name "*.so" -exec install -Dm 755 '{}' "${tldrLib}/{}" \;
find tldr -type f -name "*.json" -exec install -Dm 644 '{}' "${tldrLib}/{}" \;
retval=$?

if [ $retval != 0 ]; then
    echo '[ERROR] Failed to move to install location!'
    exit 1
fi

# Symlink executable

ln -s "${tldrLib}/tldr-sharp" "${tldrBin}/tldr-sharp"
retval=$?

if [ $retval != 0 ]; then
    echo '[ERROR] Failed to move to install location!'
    exit 1
fi

rm -rf "/tmp/tldr"

echo 'Finished'
