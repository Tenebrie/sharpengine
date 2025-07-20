$BGFX = 'V:\Taiga6\bgfx'
$SC   = "$BGFX\.build\win64_vs2022\bin\shadercRelease.exe"
$SHDR = 'V:\Taiga6\CustomEngine\Engine.Assets\Materials'
cd $SHDR

& $SC -f Meshes/HonseTerrain/HonseTerrain.frag.glsl -o ../../Engine.Editor/bin/debug/net9.0/bin/HonseTerrain.frag.bin --type fragment   --platform windows -p s_5_0 -i $BGFX\src -i V:\Taiga6\bgfx\examples\common --varyingdef varying.def.sc
& $SC -f Meshes/HonseTerrain/HonseTerrain.vert.glsl -o ../../Engine.Editor/bin/debug/net9.0/bin/HonseTerrain.vert.bin --type vertex   --platform windows -p s_5_0 -i $BGFX\src -i V:\Taiga6\bgfx\examples\common --varyingdef varying.def.sc
& $SC -f Meshes/RawColor/RawColor.frag.glsl -o ../../Engine.Editor/bin/debug/net9.0/bin/RawColor.frag.bin --type fragment   --platform windows -p s_5_0 -i $BGFX\src -i V:\Taiga6\bgfx\examples\common --varyingdef varying.def.sc
& $SC -f Meshes/RawColor/RawColor.vert.glsl -o ../../Engine.Editor/bin/debug/net9.0/bin/RawColor.vert.bin --type vertex   --platform windows -p s_5_0 -i $BGFX\src -i V:\Taiga6\bgfx\examples\common --varyingdef varying.def.sc
& $SC -f Meshes/Terrain/Terrain.frag.glsl -o ../../Engine.Editor/bin/debug/net9.0/bin/Terrain.frag.bin --type fragment   --platform windows -p s_5_0 -i $BGFX\src -i V:\Taiga6\bgfx\examples\common --varyingdef varying.def.sc
& $SC -f Meshes/Terrain/Terrain.vert.glsl -o ../../Engine.Editor/bin/debug/net9.0/bin/Terrain.vert.bin --type vertex   --platform windows -p s_5_0 -i $BGFX\src -i V:\Taiga6\bgfx\examples\common --varyingdef varying.def.sc
