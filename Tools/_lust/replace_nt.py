import re
import sys
from pathlib import Path

REPLACEMENTS = {
    r"\bNanotrasen\b": "Qillu",
    r"\bNT\b": "Qillu",
    r"\bНТ\b": "Qillu",
    r"\bНанотрейзен\b": "Qillu",
    r"\bNanoTrasen\b": "Qillu",
}

def process_ftl_files(locale_dir: Path):
    if not locale_dir.exists():
        print(f"Директория {locale_dir} не найдена, создаём её")
        locale_dir.mkdir(parents=True, exist_ok=True)

    for file_path in locale_dir.rglob("*.ftl"):
        content = file_path.read_text(encoding="utf-8")
        lines = []
        for line in content.splitlines(True):
            if "=" in line and not line.lstrip().startswith("#"):
                left, right = line.split("=", 1)
                for pattern, replacement in REPLACEMENTS.items():
                    right = re.sub(pattern, replacement, right, flags=re.IGNORECASE)
                line = left + "=" + right
            lines.append(line)
        content = "".join(lines)
        file_path.write_text(content, encoding="utf-8")
    print(f"Обработка завершена для {locale_dir}")

if __name__ == "__main__":
    if "--path" in sys.argv:
        path_index = sys.argv.index("--path") + 1
        if path_index >= len(sys.argv):
            print("Ошибка: не указан путь после --path")
            sys.exit(2)
        locale_dir = Path(sys.argv[path_index]).resolve()
    else:
        print("Ошибка: аргумент --path не указан")
        sys.exit(2)

    process_ftl_files(locale_dir)
