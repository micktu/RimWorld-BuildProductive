#!/bin/sh

MOD_PATH="/Users/micktu/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/BuildProductive/"
rm -rf "$MOD_PATH"
mkdir "$MOD_PATH"
cp -r ../{About,Assemblies,Defs,Languages,Textures} "$MOD_PATH"