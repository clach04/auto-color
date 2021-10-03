#!/bin/sh

gcc -c main.c -I/usr/include/glib-2.0 -I/usr/lib/x86_64-linux-gnu/glib-2.0/include -I../ -I../Dependencies/stb || exit $?
gcc -o auto-color-test main.o -lglib-2.0 -lgio-2.0 -lgobject-2.0 -lm || exit $?
./auto-color-test
