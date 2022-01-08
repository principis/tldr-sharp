#! /bin/bash
 set -x
cd tldr-sharp/bin/Release/net461
 
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
mono tldr_sharp.exe  --platform=linux >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test os non-existing page
mono tldr_sharp.exe giberishdsfsd --platform=linux >/dev/null
if [ $? == 0 ]; then
    exit 1
fi

# Test list-languages
mono tldr_sharp.exe --list-languages >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test list-os
mono tldr_sharp.exe --list-os >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test version
mono tldr_sharp.exe -v >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test search
mono tldr_sharp.exe -s tar >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

mono tldr_sharp.exe -s giberishdsfsd >/dev/null
if [ $? == 0 ]; then
    exit 1
fi
