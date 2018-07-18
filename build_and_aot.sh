#! /bin/sh

set -ex

config="${1:-Debug}"
framework="${2:-net472}"
path="bin/$config/$framework"
badDlls=("System.Net.Http" "System.IO.Compression" "System.Runtime.InteropServices.RuntimeInformation")

msbuild /p:Configuration="$config" /p:TargetFramework="$framework"

for i in "${badDlls[@]}"; do
    rm "$path/$i.dll"
done

for i in $(find "$path" -name "*.dll" -or -name "*.exe"); do
    sudo mono --aot "$i"
done