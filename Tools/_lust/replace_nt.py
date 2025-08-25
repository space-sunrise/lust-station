import re
import sys
from pathlib import Path

REPLACEMENTS = {
    r"\bNanotrasen\b": "Qillu",
    r"\bNanoTrasen\b": "Qillu",
    r"\bNT\b": "Qillu",
    r"\bНТ\b": "Qillu",
    r"\bНанотрейзен\b": "Qillu",
}

PLACEHOLDER_SPLIT = re.compile(r'(\{[^{}]*\})')
ATTR_RE = re.compile(r'^\s*\.[-\w]+\s*=')

def replace_outside_placeholders(text: str) -> str:
    parts = PLACEHOLDER_SPLIT.split(text)
    for i, seg in enumerate(parts):
        if seg.startswith('{') and seg.endswith('}'):
            continue
        for pat, repl in REPLACEMENTS.items():
            seg = re.sub(pat, repl, seg, flags=re.IGNORECASE)
        parts[i] = seg
    return ''.join(parts)

def process_ftl_files(locale_dir: Path):
    if not locale_dir.exists():
        print(f"Директория {locale_dir} не найдена, создаём её")
        locale_dir.mkdir(parents=True, exist_ok=True)

    for file_path in locale_dir.rglob("*.ftl"):
        content = file_path.read_text(encoding="utf-8")
        lines_out = []
        in_value = False

        for line in content.splitlines(keepends=True):
            stripped = line.lstrip()
            is_comment = stripped.startswith("#")

            if "=" in line and not is_comment and not line.startswith((" ", "\t")):
                left, right = line.split("=", 1)
                right = replace_outside_placeholders(right)
                lines_out.append(left + "=" + right)
                in_value = True
                continue

            if in_value and not is_comment:
                if ATTR_RE.match(line):
                    l2, r2 = line.split("=", 1)
                    r2 = replace_outside_placeholders(r2)
                    lines_out.append(l2 + "=" + r2)
                    continue
                if line.startswith((" ", "\t")):
                    lines_out.append(replace_outside_placeholders(line))
                    continue

            lines_out.append(line)
            if not line.startswith((" ", "\t")):
                in_value = False

        new_content = "".join(lines_out)
        if new_content != content:
            file_path.write_text(new_content, encoding="utf-8")

    print(f"Обработка завершена для {locale_dir}")

if __name__ == "__main__":
    if "--path" in sys.argv:
        idx = sys.argv.index("--path") + 1
        if idx >= len(sys.argv):
            print("Ошибка: не указан путь после --path")
            sys.exit(2)
        process_ftl_files(Path(sys.argv[idx]).resolve())
    else:
        print("Ошибка: аргумент --path не указан")
        sys.exit(2)
