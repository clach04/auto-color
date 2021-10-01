#!/bin/sh

CAKELISP_DIR=Dependencies/cakelisp

# Build Cakelisp itself
echo "\n\nCakelisp\n\n"
cd $CAKELISP_DIR
./Build.sh || exit $?

cd ../..

echo "\n\nAuto Color\n\n"

CAKELISP=./Dependencies/cakelisp/bin/cakelisp

# We can't run the full app yet until I set up SDL to build under Mingw
$CAKELISP --verbose-processes \
		  src/Config_Mingw.cake \
		  src/AutoColorCLI.cake || exit $?

rsync /usr/lib/gcc/x86_64-w64-mingw32/9.3-win32/libgcc_s_seh-1.dll .

wine AutoColor.exe
