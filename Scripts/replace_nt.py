"""
Этот скрипт автоматически находит папку Resources в корне проекта
и работает с папкой Resources/Locale.

Что делает скрипт:
1. Ищет в родительских папках проекта директорию Resources.
2. Внутри Resources создаёт папку Locale, если её ещё нет.
3. Перебирает все .ftl-файлы внутри Locale.
4. Заменяет в содержимом файлов все упоминания Nanotrasen / NT / НТ / Нанотрейзен на Qillu.
5. Перезаписывает файлы с внесёнными изменениями.

Важно:
- Оригинальные файлы в Locale перезаписываются.
"""

import re
from pathlib import Path

REPLACEMENTS = {
    r"\bNanotrasen\b": "Qillu",
    r"\bNT\b": "Qillu",
    r"\bНТ\b": "Qillu",
    r"\bНанотрейзен\b": "Qillu",
}

def find_correct_resources_dir():
    current = Path(__file__).resolve().parent
    while current != current.parent:
        resources = current / "Resources"
        if resources.exists():
            return resources
        current = current.parent
    raise FileNotFoundError("Не найдена папка Resources в родительских папках")

RESOURCES_DIR = find_correct_resources_dir()
LOCALE_DIR = RESOURCES_DIR / "Locale"

LOCALE_DIR.mkdir(parents=True, exist_ok=True)

def process_ftl_files():
    for file_path in LOCALE_DIR.rglob("*.ftl"):
        content = file_path.read_text(encoding="utf-8")
        for pattern, replacement in REPLACEMENTS.items():
            content = re.sub(pattern, replacement, content)
        file_path.write_text(content, encoding="utf-8")

if __name__ == "__main__":
    process_ftl_files()
