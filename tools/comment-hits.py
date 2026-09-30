"""Lists comment lines that likely carry history or references to documents.

Usage: python comment_hits.py <repo> [--files-only]
Prints "path:line: [D|S] text" — D = definite marker, S = suspect wording.
Only comment text is scanned: // and /// in .cs, <!-- --> in .axaml/.csproj/.props.
"""
import re
import subprocess
import sys
from pathlib import Path

DEFINITE = re.compile(
    r"docs/|\b\w+\.md\b|\bbrief|\bporcj|\bkrok(u|iem|i|ów)?\b|\bzlecen|\bzadani|\betap|\bczęść [A-Z]\b"
    r"|\bobjaw|\bdawniej|\bwcześniej|\bno longer\b|\bused to\b|\bpreviously\b|\bformerly\b"
    r"|\b[0-9a-f]{7,40}\b|\b20\d\d-\d\d-\d\d\b|\b[A-D]\d{1,2}\b|\bLibrary\.|library-to-be"
    r"|\bstep \d|\bslice\b|\bround \d|\brund[aąy]\b|\buwag[aię] autora|\bautor\w*\b|\bsekcj|\bsection \""
    r"|\"[A-ZŁŚŻŹĆ][a-ząćęłńóśźż]+(,| [a-ząćęłńóśźż]+)+\"\)|\bmockup|\bprojekt kart|\breview\b|\bpass \d",
    re.IGNORECASE)
SUSPECT = re.compile(
    r"\bnow\b|\bteraz\b|\bjuż\b|\bodkąd\b|\bmoved\b|\bprzeniesion|\bthe new\b|\bnowy\b|\bnowa\b|\bthis move\b"
    r"|\bintroduc|\brenamed\b|\breplaced\b|\bzastąpi|\bbefore this\b|\bold\b|\bstary|\bstara\b|\bthen\b.*\bnow\b"
    r"|\bafter the\b.*\b(fix|change)|\bfix(ed)?\b|\bregression\b|\bbug\b|\bwas\b|\bwere\b|\bhad been\b|\bbyło\b|\bbył\b",
    re.IGNORECASE)


def comment_lines(path: Path):
    text = path.read_text(encoding="utf-8-sig")
    lines = text.split("\n")
    if path.suffix == ".cs":
        in_block = False
        for i, line in enumerate(lines, 1):
            s = line.strip()
            if in_block:
                yield i, s
                if "*/" in s:
                    in_block = False
                continue
            if s.startswith("/*"):
                in_block = "*/" not in s
                yield i, s
            elif s.startswith("//"):
                yield i, s
            elif "//" in s and not re.search(r"https?://", s):
                idx = s.find("//")
                if s[:idx].count('"') % 2 == 0:
                    yield i, s[idx:]
    else:
        in_c = False
        for i, line in enumerate(lines, 1):
            s = line
            if in_c:
                yield i, s.strip()
                if "-->" in s:
                    in_c = False
                continue
            if "<!--" in s:
                start = s.find("<!--")
                yield i, s[start:].strip()
                in_c = "-->" not in s[start:]


def main():
    repo = Path(sys.argv[1])
    files_only = "--files-only" in sys.argv
    out = subprocess.run(["git", "-C", str(repo), "ls-files", "src", "tests"], capture_output=True, text=True).stdout.split()
    counts = {}
    for rel in out:
        if not rel.endswith((".cs", ".axaml", ".csproj")):
            continue
        p = repo / rel
        for i, s in comment_lines(p):
            kind = "D" if DEFINITE.search(s) else "S" if SUSPECT.search(s) else None
            if kind:
                counts.setdefault(rel, [0, 0])[0 if kind == "D" else 1] += 1
                if not files_only:
                    print(f"{rel}:{i}: [{kind}] {s[:200]}")
    if files_only:
        for rel, (d, s) in sorted(counts.items(), key=lambda kv: -(kv[1][0] * 3 + kv[1][1])):
            print(f"{d:4} {s:4}  {rel}")


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
