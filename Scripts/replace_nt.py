import re
import sys
from pathlib import Path

REPLACEMENTS = {
    r"Nanotrasen": "Qillu",
    r"\bNT\b": "Qillu",
    r"\bНТ\b": "Qillu",
    r"Нанотрейзен": "Qillu",
}

def replace_in_dir(target_dir: Path):
    for file_path in target_dir.rglob("*.ftl"):
        content = file_path.read_text(encoding="utf-8")
        for pattern, replacement in REPLACEMENTS.items():
            content = re.sub(pattern, replacement, content)
        file_path.write_text(content, encoding="utf-8")

if __name__ == "__main__":
    if "--path" in sys.argv:
        path_index = sys.argv.index("--path") + 1
        if path_index >= len(sys.argv):
            print("ОШИБОЧКА: нема пути после --path")
            sys.exit(1)
        target_path = Path(sys.argv[path_index]).resolve()
    else:
        current = Path(__file__).resolve().parent
        while current != current.parent:
            resources = current / "Resources"
            locale = resources / "Locale"
            if resources.exists():
                target_path = locale
                break
            current = current.parent
        else:
            raise FileNotFoundError("НЕ ВИДНА ПАПКА Resources")

    replace_in_dir(target_path)
