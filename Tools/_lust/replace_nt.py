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

REPLACEMENTS_CI = [(re.compile(p, re.IGNORECASE), r) for p, r in REPLACEMENTS.items()]
ATTR_RE = re.compile(r'^\s*\.[-\w]+\s*=')

def replace_outside_placeholders(text: str, depth: int = 0):
    out, buf = [], []
    i, n = 0, len(text)
    while i < n:
        ch = text[i]
        if ch == '{':
            if depth == 0 and buf:
                seg = ''.join(buf)
                for pat, repl in REPLACEMENTS_CI:
                    seg = pat.sub(repl, seg)
                out.append(seg)
                buf = []
            depth += 1
            out.append(ch)
        elif ch == '}' and depth > 0:
            depth -= 1
            out.append(ch)
        else:
            if depth == 0:
                buf.append(ch)
            else:
                out.append(ch)
        i += 1
    if buf:
        seg = ''.join(buf)
        for pat, repl in REPLACEMENTS_CI:
            seg = pat.sub(repl, seg)
        out.append(seg)
    return ''.join(out), depth

def process_ftl_files(locale_dir: Path):
    if not locale_dir.exists():
        print(f"Директория {locale_dir} не найдена, создаём её")
        locale_dir.mkdir(parents=True, exist_ok=True)

    for file_path in locale_dir.rglob("*.ftl"):
        content = file_path.read_text(encoding="utf-8")
        lines_out = []
        in_value = False
        ph_depth = 0

        for line in content.splitlines(keepends=True):
            stripped = line.lstrip()
            is_comment = stripped.startswith("#")

            if "=" in line and not is_comment and not line.startswith((" ", "\t")):
                left, right = line.split("=", 1)
                right, ph_depth = replace_outside_placeholders(right, 0)
                lines_out.append(left + "=" + right)
                in_value = True
                continue

            if in_value and not is_comment:
                if ATTR_RE.match(line):
                    l2, r2 = line.split("=", 1)
                    r2, _ = replace_outside_placeholders(r2, 0)
                    lines_out.append(l2 + "=" + r2)
                    continue
                if line.startswith((" ", "\t")):
                    seg, ph_depth = replace_outside_placeholders(line, ph_depth)
                    lines_out.append(seg)
                    continue

            lines_out.append(line)
            if is_comment or not line.startswith((" ", "\t")) or stripped == "":
                in_value = False
                ph_depth = 0

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
