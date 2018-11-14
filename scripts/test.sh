#! /bin/bash
 
cd tldr-sharp/bin/Release
 
chmod +x tldr_sharp.exe

# Test update
./tldr_sharp.exe -u >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
./tldr_sharp.exe --update >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test clear cache
./tldr_sharp.exe -c >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
./tldr_sharp.exe --clear-cache >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test list
./tldr_sharp.exe --list >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test list-all
./tldr_sharp.exe -a >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
./tldr_sharp.exe --list-all >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
./tldr_sharp.exe tar >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test help
./tldr_sharp.exe -h >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
./tldr_sharp.exe --help >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test non-existing command
./tldr_sharp.exe -x >/dev/null
if [ $? == 0 ]; then
    exit 1
fi
./tldr_sharp.exe --fake >/dev/null
if [ $? == 0 ]; then
    exit 1
fi
 
# Test page
./tldr_sharp.exe tar >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
# Test non-existing page
./tldr_sharp.exe giberishdsfsd >/dev/null
if [ $? == 0 ]; then
    exit 1
fi 

# test os 
./tldr_sharp.exe tldr --os=linux >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test os non-existing page
./tldr_sharp.exe dir --os=linux >/dev/null
if [ $? == 0 ]; then
    exit 1
fi

# Test list-languages
./tldr_sharp.exe tldr --list-languages >/dev/null
if [ $? != 0 ]; then
    exit 1
fi

# Test list-os
./tldr_sharp.exe tldr --list-os >/dev/null
if [ $? != 0 ]; then
    exit 1
fi