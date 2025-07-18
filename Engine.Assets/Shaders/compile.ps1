$BGFX = 'V:\Taiga6\bgfx'
$SC   = "$BGFX\.build\win64_vs2022\bin\shadercRelease.exe"
$SHDR = 'V:\Taiga6\CustomEngine\Engine.Assets\Materials'
cd $SHDR

& $SC -f cube.vert.sc -o ../../Engine.Editor/bin/debug/net9.0/bin/cube.vert.bin --type vertex   --platform windows -p s_5_0 -i $BGFX\src -i V:\Taiga6\bgfx\examples\common --varyingdef varying.def.sc
& $SC -f cube.frag.sc -o ../../Engine.Editor/bin/debug/net9.0/bin/cube.frag.bin --type fragment --platform windows -p s_5_0 -i $BGFX\src -i V:\Taiga6\bgfx\examples\common --varyingdef varying.def.sc
& $SC -f terrain.vert.glsl -o ../../Engine.Editor/bin/debug/net9.0/bin/terrain.vert.bin --type vertex   --platform windows -p s_5_0 -i $BGFX\src -i V:\Taiga6\bgfx\examples\common --varyingdef varying.def.sc
& $SC -f terrain.frag.glsl -o ../../Engine.Editor/bin/debug/net9.0/bin/terrain.frag.bin --type fragment --platform windows -p s_5_0 -i $BGFX\src -i V:\Taiga6\bgfx\examples\common --varyingdef varying.def.sc
