#!/bin/sh
echo "C# code:"
cat `find Game | grep \\.cs$` | wc

echo "Lua code:"
cat `find Game | grep \\.lua$` | wc

echo "GLSL code:"
cat `find Game | grep 'frag\|vert'` | wc

echo "Shell code:"
cat `find . | grep \\.sh$` | wc
