import re
import sys
from pathlib import Path

REPLACEMENTS = {
    r"\bNanotrasen\b": "Qillu",
    r"\bNT\b": "Qillu",
    r"\bНТ\b": "Qillu",
    r"\bНанотрейзен\b": "Qillu",
}

def process_ftl_files(locale_dir: Path):
    for file_path in locale_dir.rglob("*.ftl"):
        content = file_path.read_text(encoding="utf-8")
        for pattern, replacement in REPLACEMENTS.items():
            content = re.sub(pattern, replacement, content)
        file_path.write_text(content, encoding="utf-8")

if __name__ == "__main__":
    if "--path" in sys.argv:
        path_index = sys.argv.index("--path") + 1
        if path_index >= len(sys.argv):
            print("Ошибка: путь не указан после --path")
            sys.exit(1)
        locale_dir = Path(sys.argv[path_index]).resolve()
    else:
        print("Ошибка: не указан --path")
        sys.exit(1)

    if not locale_dir.exists():
        print(f"Ошибка: директория {locale_dir} не существует")
        sys.exit(1)

    process_ftl_files(locale_dir)
