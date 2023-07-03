#!/bin/sh

read -p "Have you built the game in Release and ReleaseWindows mode? " PROMPT
if [ $PROMPT != "yes" ]
then
	echo "You should do that first."
	exit
fi 

TITLE="TankCommando3D"
VERSION=`monodis --assembly Game/bin/Release/Game.exe | grep Version | egrep -o "[0-9]+.[0-9]+.[0-9]+"`
SVN="svn"

echo "Preparing directories"
rm -rf Deploy
mkdir Deploy

function prepare {
	echo "Preparing ${1} files"
	mkdir Deploy/${1}

	# Game
	cp Game/SDL2-CS.dll Deploy/${1}
	cp Game/MiniTK.dll Deploy/${1}
	cp Game/Ionic.Zip.dll Deploy/${1}
	if [[ ${1} =~ "Win" ]]; then
		cp Game/bin/ReleaseWindows/Game.exe Deploy/${1}
	else
		cp Game/bin/Release/Game.exe Deploy/${1}
		cp Game/SDL2-CS.dll.config Deploy/${1}
		cp Game/MiniTK.dll.config Deploy/${1}
	fi
	$SVN export -q Game/assets Deploy/${1}/assets

	if [[ ${1} =~ "Win" ]]; then
		# Native DLLS
		$SVN export -q Game/Natives/${1} Deploy/Temp
		cp -r Deploy/Temp/* Deploy/${1}
		rm -rf Deploy/Temp

		# Remove Steamworks
		rm -f Deploy/${1}/CSteamworks.dll
		rm -f Deploy/${1}/steam_api.dll

		# Rename .exe
		mv Deploy/${1}/Game.exe Deploy/${1}/${TITLE}.exe
	else
		# MonoKickStart
		$SVN export -q KickStart Deploy/Temp
		cp -r Deploy/Temp/* Deploy/${1}
		rm -rf Deploy/Temp
		if [[ ${1} =~ "OSX" ]]; then
			rm Deploy/${1}/Game.bin.x86
			rm Deploy/${1}/Game.bin.x86_64
		fi
		if [[ ${1} =~ "Linux" ]]; then
			rm Deploy/${1}/Game.bin.osx
			mv Deploy/${1}/Game Deploy/${1}/${TITLE}
		fi

		# Native DLLS
		if [[ ${1} =~ "OSX" ]]; then
			$SVN export -q Game/Natives/${1} Deploy/${1}/osx
		fi
		if [[ ${1} =~ "Linux" ]]; then
			$SVN export -q Game/Natives/${1}32 Deploy/${1}/lib
			$SVN export -q Game/Natives/${1}64 Deploy/${1}/lib64
		fi

		# Remove Steamworks
		if [[ ${1} =~ "OSX" ]]; then
			rm -f Deploy/${1}/libCSteamworks.dylib
			rm -f Deploy/${1}/libsteam_api.dylib
		fi
		if [[ ${1} =~ "Linux" ]]; then
			rm -f Deploy/${1}/libCSteamworks.so
			rm -f Deploy/${1}/libsteam_api.so
		fi
	fi
}

function package_zipped_bundle {
	if [ -d "Deploy/${1}" ]; then
		echo "Packaging for ${1}"
		$SVN export -q AppBundle Deploy/${1}Bundle
		mv Deploy/${1}Bundle/Game.app Deploy/${1}Bundle/${TITLE}.app
		cp -r Deploy/${1}/* Deploy/${1}Bundle/${TITLE}.app/Contents/MacOS
		cp Game/Icons/Icon.icns Deploy/${1}Bundle/${TITLE}.app/Contents/Resources

		cd Deploy/${1}Bundle
		zip -rq ../${TITLE}_${2}_${VERSION}.zip ${TITLE}.app
        cd ..
        md5 ${TITLE}_${2}_${VERSION}.zip >> md5_hashes.txt
        cd ..
	fi
}

function package_zip {
	if [ -d "Deploy/${1}" ]; then
		echo "Packaging for ${1}"
		cd Deploy/${1}
		zip -rq ../${TITLE}_${2}_${VERSION}.zip *
        cd ..
        md5 ${TITLE}_${2}_${VERSION}.zip >> md5_hashes.txt
        cd ..		
	fi
}

prepare OSX
prepare Win32
prepare Linux

touch Deploy/md5_hashes.txt
package_zipped_bundle OSX OSX
package_zip Win32 Windows
package_zip Linux Linux
