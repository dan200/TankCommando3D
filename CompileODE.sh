#!/bin/sh

ODE_PATH="./ode-0.15.2"
if [ ! -d ${ODE_PATH} ]; then
	echo "ODE not found (Extract to ${ODE_PATH})"
	exit
fi

GAME_PATH=`pwd`
UNAME=`uname`

cd ${ODE_PATH}/build
premake4 --with-libccd --no-builtin-threading-impl gmake
cd gmake
make config=debugsingledll clean
make config=debugsingledll ode
make config=releasesingledll clean
make config=releasesingledll ode

cd ${GAME_PATH}
if [ "$UNAME" == "Darwin" ]; then
	# OSX
	cp ${ODE_PATH}/lib/ReleaseSingleDLL/libode_single.dylib Game/Natives/OSX
	cp ${ODE_PATH}/lib/DebugSingleDLL/libode_singled.dylib Game/bin/Debug
else
	# Linux
	echo "TODO: Linux"
fi
