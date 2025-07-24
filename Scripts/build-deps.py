#!/usr/bin/env python3
"""
compile_dependencies.py

Cross-platform build for bgfx and its tools (shaderc, texturec, etc.) in Submodules/bgfx.
On macOS: uses Makefile (make osx-release, make osx-debug).
On Windows: generates a VS2022 solution via GENie (from bx) with shared-lib, locates MSBuild.exe automatically, builds both Debug and Release, then reports DLL and LIB locations.
Script resides in Scripts/ and can be invoked from any folder.
"""

import sys
import shutil
import subprocess
from pathlib import Path


def err(msg):
    print(f"Error: {msg}", file=sys.stderr)
    sys.exit(1)


def find_msbuild():
    # Try msbuild on PATH
    msbuild = shutil.which("msbuild")
    if msbuild:
        return msbuild
    # Fallback: locate via vswhere
    vswhere_path = Path(r"C:/Program Files (x86)/Microsoft Visual Studio/Installer/vswhere.exe")
    if vswhere_path.is_file():
        try:
            output = subprocess.check_output([
                str(vswhere_path),
                "-latest",
                "-requires", "Microsoft.Component.MSBuild",
                "-find", "MSBuild\\**\\Bin\\MSBuild.exe"
            ], universal_newlines=True)
            for line in output.splitlines():
                candidate = Path(line.strip())
                if candidate.is_file():
                    return str(candidate)
        except subprocess.CalledProcessError:
            pass
    return None


def build_macos(bgfx_root):
    for cfg, target in [("Debug", "osx-debug"), ("Release", "osx-release")]:
        cmd = ["make", target]
        print(f"→ Running: {' '.join(cmd)} in {bgfx_root}")
        subprocess.check_call(cmd, cwd=str(bgfx_root))
        out_dir = bgfx_root / ".build" / target.split('-')[1]
        print(f"✅ macOS {cfg} build complete. Binaries under: {out_dir}")


def build_windows(bgfx_root, bx_root):
    genie = bx_root / "tools" / "bin" / "windows" / "genie.exe"
    if not genie.is_file():
        err(f"GENie not found at {genie!r}. Did you clone and update bx submodule?")

    # Always generate a VS2022 solution (defaults to x64)
    action = "vs2022"
    gen_cmd = [str(genie), "--with-tools", "--with-shared-lib", action]
    print(f"→ Generating {action} solution: {' '.join(gen_cmd)}")
    subprocess.check_call(gen_cmd, cwd=str(bgfx_root))

    # Locate solution file
    sln = next((bgfx_root / ".build").rglob("*.sln"), None)
    if not sln:
        err("Solution (.sln) not found under .build")

    msbuild = find_msbuild()
    if not msbuild:
        err("msbuild.exe not found. Install Visual Studio Build Tools or run in a VS Developer Prompt.")

    for cfg in ("Debug", "Release"):
        print(f"→ Building Windows {cfg} ({action})")
        build_cmd = [msbuild, str(sln), f"/p:Configuration={cfg}", "/p:Platform=x64", "/m"]
        print(f"  {' '.join(build_cmd)}")
        subprocess.check_call(build_cmd)

    artifacts = list((bgfx_root / ".build").rglob("*.dll"))
    if artifacts:
        print("✅ Windows builds complete. Artifacts:")
        for a in artifacts:
            print("  DLL:", a)
    else:
        print("⚠️ Builds succeeded but no DLLs or LIBs found.")


def main():
    script = Path(__file__).resolve()
    repo = script.parent.parent

    bgfx = repo / "Submodules" / "bgfx"
    bx   = repo / "Submodules" / "bx"
    if not bgfx.is_dir():
        err(f"Submodules/bgfx not found: {bgfx}")
    if not bx.is_dir():
        err(f"Submodules/bx not found: {bx}")

    plat = sys.platform
    try:
        if plat == "darwin":
            build_macos(bgfx)
        elif plat.startswith("win"):
            build_windows(bgfx, bx)
        else:
            err(f"Unsupported platform: {plat}")
    except subprocess.CalledProcessError as e:
        err(f"Build failed (exit code {e.returncode})")

if __name__ == "__main__":
    main()
