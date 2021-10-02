#+/bin/sh

CAKELISP_DIR=Dependencies/cakelisp

# Build Cakelisp itself
echo "\n\nCakelisp\n\n"
cd $CAKELISP_DIR
./Build.sh || exit $?

cd ../..

echo "\n\nAuto Color\n\n"

CAKELISP=./Dependencies/cakelisp/bin/cakelisp

$CAKELISP --verbose-processes \
		  src/Config_Linux.cake \
		  src/AutoColorCLI.cake || exit $?

valgrind --leak-check=full --suppressions=glib.supp \
		 ./auto-color
