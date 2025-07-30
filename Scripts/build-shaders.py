#!/usr/bin/env python3
import argparse
import subprocess
import sys
import time
from pathlib import Path

from colorama import Fore, Style, init

# initialize colorama
init(autoreset=True)

def write_section(text: str, color: str = "CYAN"):
    fg = getattr(Fore, color.upper(), Fore.CYAN)
    print()
    print(fg + text)
    print(fg + ("-" * len(text)))


def write_result(src: str, dst: str, ok: bool, pad: int):
    flag = "[OK]" if ok else "[!!]"
    color = Fore.GREEN if ok else Fore.RED
    src_padded = src.ljust(pad)
    print(color + flag + Style.RESET_ALL + f" {src_padded} -> {dst}")


def invoke_compile_shaders(shdr_dir: Path, out_base: Path,
                           compiler: Path, common_params: list,
                           pattern: str, shader_type: str, errors: list):
    write_section(f"-- Compiling {shader_type} shaders")
    files = list(shdr_dir.rglob(pattern))
    # compute pad width for consistent columns
    max_len = max((len(str(f.relative_to(shdr_dir))) for f in files), default=0) + 2

    count = 0
    for src_path in files:
        rel = str(src_path.relative_to(shdr_dir))
        out_rel = rel[:-5] + ".bin"  # strip ".glsl", add ".bin"
        out_path = out_base / out_rel
        out_path.parent.mkdir(parents=True, exist_ok=True)

        cmd = [str(compiler), "-f", str(src_path), "-o", str(out_path),
               "--type", shader_type] + common_params
        proc = subprocess.run(cmd, capture_output=True, text=True)
        ok = (proc.returncode == 0)
        if proc.stderr:
            errors.extend(proc.stderr.strip().splitlines())

        write_result(rel, out_rel, ok, max_len)
        if not ok:
            for line in proc.stderr.strip().splitlines():
                print(Fore.YELLOW + line)
            for line in proc.stdout.strip().splitlines():
                print(Fore.YELLOW + line)
        count += 1

    return count


def main():
    parser = argparse.ArgumentParser(
        description="Compile BGFX shaders (vertex + fragment)."
    )
    parser.add_argument(
        "--config", "-c",
        choices=["debug", "release"],
        default="debug",
        help="Build configuration"
    )
    parser.add_argument(
        "--framework", "-f",
        default="net9.0",
        help="Target framework"
    )
    args = parser.parse_args()

    # roots
    script_dir = Path(__file__).parent.resolve()
    solution_root = script_dir.parent

    bgfx = (solution_root / "Submodules" / "bgfx").resolve()
    if not bgfx.is_dir():
        print(Fore.RED +
              f"bgfx folder not found next to {solution_root}; adjust BGFX path.")
        sys.exit(1)

    # fixed paths
    compiler = bgfx / ".build" / "win64_vs2022" / "bin" / "shadercRelease.exe"
    shdr_dir = solution_root / "Engine.Assets" / "Materials"
    out_base = (solution_root / "Engine.Editor" / "bin" /
                args.config / args.framework / "Compiled" / "Shaders")

    # common params
    common_params = [
        "--platform", "windows",
        "-p", "s_5_0",
        "-i", str(bgfx / "src"),
        "-i", str(bgfx / "examples" / "common"),
        "--varyingdef", str(shdr_dir / "varying.def.sc")
    ]

    errors = []
    start = time.perf_counter()

    v_cnt = invoke_compile_shaders(
        shdr_dir, out_base, compiler, common_params,
        pattern="*.vert.glsl", shader_type="vertex", errors=errors
    )
    f_cnt = invoke_compile_shaders(
        shdr_dir, out_base, compiler, common_params,
        pattern="*.frag.glsl", shader_type="fragment", errors=errors
    )

    elapsed = time.perf_counter() - start
    print()
    print(Fore.CYAN +
          f"Done: {v_cnt} vertex + {f_cnt} fragment shaders in {elapsed:.2f} s")

    if errors:
        write_section("-- Errors", color="RED")
        for line in errors:
            print(Fore.RED + line)


if __name__ == "__main__":
    main()