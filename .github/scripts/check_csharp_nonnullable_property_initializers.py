#!/usr/bin/env python3
import pathlib
import re
import sys


PROPERTY_WITH_DEFAULT_BANG = re.compile(
    r"^\s*(?:(?:public|internal|protected|private)\s+)?(?:required\s+)?(?P<type>[A-Za-z_][A-Za-z0-9_<>,?.\[\]]*)\s+[A-Za-z_][A-Za-z0-9_]*\s*\{\s*get\s*;\s*set\s*;\s*\}\s*=\s*default!\s*;\s*$",
    re.MULTILINE,
)


KNOWN_VALUE_TYPES = {
    "bool",
    "byte",
    "sbyte",
    "short",
    "ushort",
    "int",
    "uint",
    "long",
    "ulong",
    "nint",
    "nuint",
    "float",
    "double",
    "decimal",
    "char",
    "DateOnly",
    "TimeOnly",
    "DateTime",
    "DateTimeOffset",
    "TimeSpan",
    "Guid",
}


def is_known_value_type(type_name: str) -> bool:
    normalized = type_name.rstrip("?")
    normalized = normalized.split("<", 1)[0]
    normalized = normalized.split(".")[-1]
    return normalized in KNOWN_VALUE_TYPES


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
        if any(part in {"bin", "obj"} for part in file_path.parts):
            continue

        relative_path = file_path.relative_to(repo_root)
        content = file_path.read_text(encoding="utf-8", errors="ignore")

        for line_number, line in enumerate(content.splitlines(), start=1):
            match = PROPERTY_WITH_DEFAULT_BANG.match(line)

            if not match:
                continue

            type_name = match.group("type")

            if is_known_value_type(type_name):
                continue

            failures.append(
                f"{relative_path}:{line_number}: non-nullable reference-type property initializer uses 'default!'; use 'null!' instead"
            )

    if failures:
        print("Non-nullable property initializer convention violations found:")
        for failure in failures:
            print(f" - {failure}")
        return 1

    print("Non-nullable property initializer convention check passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
