#! /bin/bash
 set -x
cd tldr-sharp/bin/Release
 
chmod +x tldr_sharp.exe

# Test update
mono tldr_sharp.exe -u >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
mono tldr_sharp.exe --update >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test clear cache
mono tldr_sharp.exe -c >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
mono tldr_sharp.exe --clear-cache >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test list
mono tldr_sharp.exe --list >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test list-all
mono tldr_sharp.exe -a >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
mono tldr_sharp.exe --list-all >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
mono tldr_sharp.exe tar >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test help
mono tldr_sharp.exe -h >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
mono tldr_sharp.exe --help >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test non-existing command
mono tldr_sharp.exe -x >/dev/null
if [ $? == 0 ]; then
    exit 1
fi
mono tldr_sharp.exe --fake >/dev/null
if [ $? == 0 ]; then
    exit 1
fi
 
# Test page
mono tldr_sharp.exe tar >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test non-existing page
mono tldr_sharp.exe giberishdsfsd >/dev/null
if [ $? == 0 ]; then
    exit 1
fi 

# test os 
mono tldr_sharp.exe tldr --platform=linux >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test os non-existing page
mono tldr_sharp.exe giberishdsfsd --platform=linux >/dev/null
if [ $? == 0 ]; then
    exit 1
fi

# Test list-languages
mono tldr_sharp.exe tldr --list-languages >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test list-os
mono tldr_sharp.exe tldr --list-os >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test version
mono tldr_sharp.exe tldr -v >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

