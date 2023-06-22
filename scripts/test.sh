#! /bin/bash

# SPDX-FileCopyrightText: None
# SPDX-License-Identifier: CC0-1.0

set -x
cd publish/linux-x64

TLDR='./tldr-sharp'
chmod +x $TLDR

# Test update
$TLDR -u >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
$TLDR --update >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test clear cache
$TLDR -c >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
$TLDR --clear-cache >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test list
$TLDR --list >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test list-all
$TLDR -a >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
$TLDR --list-all >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

$TLDR tar >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test help
$TLDR -h >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
$TLDR --help >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test non-existing command
$TLDR -x >/dev/null
if [ $? == 0 ]; then
    exit 1
fi
$TLDR --fake >/dev/null
if [ $? == 0 ]; then
    exit 1
fi

# Test page
$TLDR tar >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test non-existing page
$TLDR giberishdsfsd >/dev/null
if [ $? == 0 ]; then
    exit 1
fi

# test os
$TLDR --platform=linux >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test os non-existing page
$TLDR giberishdsfsd --platform=linux >/dev/null
if [ $? == 0 ]; then
    exit 1
fi

# Test list-languages
$TLDR --list-languages >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test list-platforms
$TLDR --list-platforms >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test version
$TLDR -v >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test search
$TLDR -s tar >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

$TLDR -s giberishdsfsd >/dev/null
if [ $? == 0 ]; then
    exit 1
fi
