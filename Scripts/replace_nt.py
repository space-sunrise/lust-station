import os
import re
from pathlib import Path

# Заменяемые НТ на Qillu
REPLACEMENTS = {
    r"\bNanotrasen\b": "Qillu",
    r"\bNT\b": "Qillu",
    r"\bНТ\b": "Qillu",
    r"\bНанотрейзен\b": "Qillu",
}

# Папки с исходными/изменёнными локализациями
ROOT_DIR = Path(__file__).resolve().parent.parent / "Resources" / "Locale"
TEMP_DIR = Path(__file__).resolve().parent.parent / "TempLocale"

def copy_ftl_files():
    if TEMP_DIR.exists():
        return

    for src_path in ROOT_DIR.rglob("*.ftl"):
        dest_path = TEMP_DIR / src_path.relative_to(ROOT_DIR)
        dest_path.parent.mkdir(parents=True, exist_ok=True)
        dest_path.write_text(src_path.read_text(encoding="utf-8"), encoding="utf-8")

def replace_in_files():
    for file_path in TEMP_DIR.rglob("*.ftl"):
        content = file_path.read_text(encoding="utf-8")
        for pattern, replacement in REPLACEMENTS.items():
            content = re.sub(pattern, replacement, content)
        file_path.write_text(content, encoding="utf-8")

if __name__ == "__main__":
    copy_ftl_files()
    replace_in_files()

# Результат папка TempLocale с полным переводом на Qillu
