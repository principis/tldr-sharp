#! /bin/bash
 
cd tldr-sharp/bin/Release
 
chmod +x tldr_sharp.exe
./tldr_sharp.exe -u >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
./tldr_sharp.exe -c >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
./tldr_sharp.exe --update >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
./tldr_sharp.exe --clear-cache >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
./tldr_sharp.exe --list >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
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
 
./tldr_sharp.exe -h >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
./tldr_sharp.exe --help >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
./tldr_sharp.exe -k >/dev/null
if [ $? == 0 ]; then
    exit 1
fi
 
./tldr_sharp.exe tar >/dev/null
if [ $? != 0 ]; then
    exit 1
fi
 
./tldr_sharp.exe giberishdsfsd >/dev/null
if [ $? == 0 ]; then
    exit 1
fi
