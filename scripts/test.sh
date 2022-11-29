#! /bin/bash

# SPDX-FileCopyrightText: None
# SPDX-License-Identifier: CC0-1.0

set -x
cd tldr-sharp/bin/Release/net461
 
chmod +x tldr-sharp.exe

# Test update
mono tldr-sharp.exe -u >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
mono tldr-sharp.exe --update >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test clear cache
mono tldr-sharp.exe -c >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
mono tldr-sharp.exe --clear-cache >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test list
mono tldr-sharp.exe --list >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test list-all
mono tldr-sharp.exe -a >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
mono tldr-sharp.exe --list-all >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
mono tldr-sharp.exe tar >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test help
mono tldr-sharp.exe -h >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
mono tldr-sharp.exe --help >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test non-existing command
mono tldr-sharp.exe -x >/dev/null
if [ $? == 0 ]; then
    exit 1
fi
mono tldr-sharp.exe --fake >/dev/null
if [ $? == 0 ]; then
    exit 1
fi
 
# Test page
mono tldr-sharp.exe tar >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test non-existing page
mono tldr-sharp.exe giberishdsfsd >/dev/null
if [ $? == 0 ]; then
    exit 1
fi 

# test os 
mono tldr-sharp.exe  --platform=linux >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test os non-existing page
mono tldr-sharp.exe giberishdsfsd --platform=linux >/dev/null
if [ $? == 0 ]; then
    exit 1
fi

# Test list-languages
mono tldr-sharp.exe --list-languages >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test list-os
mono tldr-sharp.exe --list-os >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test version
mono tldr-sharp.exe -v >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test search
mono tldr-sharp.exe -s tar >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

mono tldr-sharp.exe -s giberishdsfsd >/dev/null
if [ $? == 0 ]; then
    exit 1
fi
