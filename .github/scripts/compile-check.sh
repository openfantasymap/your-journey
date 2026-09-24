#!/bin/bash
# Compile YourJourney's scripts against the Unity editor's own assemblies and the
# package versions pinned in Packages/manifest.json. Needs no Unity license, so it
# runs on every push/PR. Run inside the unityci/editor base image:
#   docker run --rm -v "$PWD":/proj unityci/editor:ubuntu-<version>-base-3 bash /proj/.github/scripts/compile-check.sh
set -euo pipefail

PROJ=${PROJ:-/proj}/YourJourney
U=/opt/unity/Editor/Data
CSC="$U/NetCoreRuntime/dotnet $U/DotNetSdkRoslyn/csc.dll"
PKGS=$U/Resources/PackageManager/Editor
API=$U/UnityReferenceAssemblies/unity-4.8-api
W=$(mktemp -d)

pkgver() { sed -n "s/.*\"$1\": \"\([^\"]*\)\".*/\1/p" "$PROJ/Packages/manifest.json"; }
unpack() { mkdir -p "$W/$1"; tar xzf "$PKGS/$1-$(pkgver "$1").tgz" -C "$W/$1"; }
unpack com.unity.nuget.newtonsoft-json
unpack com.unity.postprocessing

REFS=""
for f in mscorlib System System.Core System.Xml System.Data System.Runtime.Serialization System.IO.Compression System.IO.Compression.FileSystem Facades/netstandard; do
	REFS="$REFS -r:$API/$f.dll"
done
for f in "$U"/Managed/UnityEngine/*.dll; do REFS="$REFS -r:$f"; done

DEFINES="-define:UNITY_STANDALONE -define:UNITY_POST_PROCESSING_STACK_V2 -define:ENABLE_VR_MODULE -define:ENABLE_XR_MODULE"
for v in 5_3 5_4 5_5 5_6 2017_1 2017_2 2017_3 2017_4 2018_1 2018_2 2018_3 2018_4 2019_1 2019_2 2019_3 2019_4 \
	2020_1 2020_2 2020_3 2021_1 2021_2 2021_3 2022_1 2022_2 2022_3 2023_1 2023_2 6000_0; do
	DEFINES="$DEFINES -define:UNITY_${v}_OR_NEWER"
done
# obsolete-API warnings (0618) are expected from the 2019-era code and DOTween modules
COMMON="-nologo -noconfig -nostdlib -unsafe -langversion:9 -t:library -nowarn:0618,0414,0649,0169,0219,0162,0168,1591 $DEFINES"

compile() { # name, extra args..., files
	local name=$1; shift
	echo "::group::compile $name"
	if ! $CSC $COMMON $REFS -out:"$W/$name.dll" "$@" > "$W/$name.log" 2>&1; then
		grep -E 'error' "$W/$name.log" || cat "$W/$name.log"
		echo "::endgroup::"
		echo "::error::$name failed to compile"
		exit 1
	fi
	grep -cE 'warning' "$W/$name.log" | sed 's/^/warnings: /' || true
	echo "::endgroup::"
}

compile UnityEngine.UI $(find "$U/Resources/PackageManager/BuiltInPackages/com.unity.ugui/Runtime/UGUI" -name '*.cs')
compile Unity.Postprocessing.Runtime $(find "$W/com.unity.postprocessing/package/PostProcessing/Runtime" -name '*.cs')

NJ="$W/com.unity.nuget.newtonsoft-json/package/Runtime/Newtonsoft.Json.dll"
GAME_REFS="-r:$W/UnityEngine.UI.dll -r:$W/Unity.Postprocessing.Runtime.dll -r:$NJ -r:$PROJ/Assets/Demigiant/DOTween/DOTween.dll"
compile Assembly-CSharp $GAME_REFS $(find "$PROJ/Assets" -name '*.cs' -not -path '*/Editor/*')
compile Assembly-CSharp-Editor $GAME_REFS -r:"$W/Assembly-CSharp.dll" -define:UNITY_EDITOR $(find "$PROJ/Assets/Editor" -name '*.cs')

echo "All scripts compile against Unity $(sed -n 's/m_EditorVersion: //p' "$PROJ/ProjectSettings/ProjectVersion.txt")"
