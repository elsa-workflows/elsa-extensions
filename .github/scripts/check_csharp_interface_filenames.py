#!/usr/bin/env python3
import pathlib
import re
import sys


INTERFACE_REGEX = re.compile(
    r"^\s*(?:(?:public|internal|protected|private)\s+)?(?:partial\s+)?interface\s+([A-Za-z_][A-Za-z0-9_]*)\b",
    re.MULTILINE,
)


def main() -> int:
    repo_root = pathlib.Path(__file__).resolve().parents[2]
    source_root = repo_root / "src"

    failures: list[str] = []

    if len(sys.argv) > 1:
        candidate_files = []
        for arg in sys.argv[1:]:
            file_path = (repo_root / arg).resolve() if not pathlib.Path(arg).is_absolute() else pathlib.Path(arg).resolve()
            if not file_path.exists() or file_path.suffix != ".cs":
                continue
            if source_root not in file_path.parents:
                continue
            candidate_files.append(file_path)
    else:
        candidate_files = list(source_root.rglob("*.cs"))

    for file_path in candidate_files:
        relative_path = file_path.relative_to(repo_root)

        if any(part in {"bin", "obj"} for part in file_path.parts):
            continue

        content = file_path.read_text(encoding="utf-8", errors="ignore")
        interface_names = INTERFACE_REGEX.findall(content)

        if not interface_names:
            continue

        filename = file_path.stem

        if filename not in interface_names:
            failures.append(
                f"{relative_path}: filename '{filename}.cs' does not match any declared interface name {interface_names}"
            )

    if failures:
        print("Interface filename convention violations found:")
        for failure in failures:
            print(f" - {failure}")
        return 1

    print("Interface filename convention check passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
