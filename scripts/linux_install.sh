#!/bin/bash

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

ARCH=$(uname -m)
if [ "$ARCH" == 'x86_64' ]; then
  ARCH="_x64"
elif [ "$ARCH" == 'i686' ]; then
  ARCH=""
else
  echo "[ERROR] $ARCH is not supported!"
  exit 1
fi

# Download release

mkdir tldr
wget -q "https://github.com/principis/tldr-sharp/releases/latest/download/tldr-sharp_linux${ARCH}.tar.gz" -O tldr-sharp.tar.gz
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

find tldr -not -name "*.exe" -type f -exec install -Dm 644 '{}' "${tldrLib}/{}" \;
find tldr -name '*.exe' -type f -exec install -Dm 755 '{}' "${tldrLib}/{}" \;
retval=$?

if [ $retval != 0 ]; then
    echo '[ERROR] Failed to move to install location!'
    exit 1
fi

rm -rf "/tmp/tldr"

# Download executable

wget -q https://raw.githubusercontent.com/principis/tldr-sharp/main/tldr
retval=$?

if [ $retval != 0 ]; then
    echo '[ERROR] Failed to download tldr executable!'
    exit 1
fi

# Install executable

install -m755 tldr "$tldrBin"
retval=$?

if [ $retval != 0 ]; then
    echo '[ERROR] Failed to move to install location!'
    exit 1
fi

# Delete tmp
rm tldr

echo 'Finished'
