#!/bin/sh
SVN="svn"

echo "Preparing directories"
function setup {
	rm -rf Game/bin/${1}
	$SVN export -q Game/Natives/OSX Game/bin/${1}
	if [[ ${1} =~ "SteamDebug" ]]; then
		cp Game/steam_appid.txt Game/bin/${1}
	fi
}

setup Debug
setup Release
setup SteamDebug
setup SteamRelease
