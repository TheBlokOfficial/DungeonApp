"""Zużycie subagentów z zapisów Claude Code — kroki, czas, odczyt, zapis.

Służy do porównania modeli i sposobów zlecania pracy subagentom (wpis „Model subagentów”
w docs/decisions.md). Czyta ~/.claude/projects/D--Projekty-DungeonApp/<sesja>/subagents/*.jsonl (pole usage).

Definicje:
  krok     — jedno wywołanie modelu (unikalne message.id; jedna odpowiedź zajmuje w zapisie
             kilka linii, po jednej na blok treści, z tym samym usage),
  czas     — suma odstępów między kolejnymi wpisami, bez przerw dłuższych niż 10 minut
             (przerwa to czekanie na wznowienie, nie praca),
  odczyt   — cache_read_input_tokens, zapis — output_tokens, zapis do cache — cache_creation,
  z kodem  — przebieg, który edytował pliki .cs/.axaml/.csproj/.props,
  interfejs — przebieg, który edytował .axaml albo plik widoku (*View*.cs, Theme, Styles).

Użycie:
  python tools/subagent-usage.py                       # wszystkie przebiegi
  python tools/subagent-usage.py --since 2026-09-22    # od dnia (UTC)
  python tools/subagent-usage.py --session <id>        # jedna sesja architekta
  python tools/subagent-usage.py --ui --top 10
"""

import argparse
import json
import statistics
import sys
from datetime import datetime, timedelta
from pathlib import Path

ROOT = Path.home() / ".claude" / "projects" / "D--Projekty-DungeonApp"
EDIT_TOOLS = {"Edit", "Write", "MultiEdit", "NotebookEdit"}
CODE_EXT = (".cs", ".axaml", ".csproj", ".props")
IDLE_GAP = timedelta(minutes=10)


def parse_ts(s):
    return datetime.fromisoformat(s.replace("Z", "+00:00"))


def is_ui_path(p):
    name = Path(p).name
    return p.endswith(".axaml") or (p.endswith(".cs") and ("View" in name or "Theme" in name or "Style" in name))


def read_run(path):
    usage, stamps, edited = {}, [], set()
    with open(path, encoding="utf-8") as f:
        for line in f:
            try:
                rec = json.loads(line)
            except json.JSONDecodeError:
                continue
            if "timestamp" in rec:
                stamps.append(parse_ts(rec["timestamp"]))
            if rec.get("type") != "assistant":
                continue
            msg = rec.get("message") or {}
            if msg.get("id") and msg.get("usage"):
                usage[msg["id"]] = (msg["usage"], msg.get("model", "?"))
            for block in msg.get("content") or []:
                if isinstance(block, dict) and block.get("type") == "tool_use" and block.get("name") in EDIT_TOOLS:
                    p = (block.get("input") or {}).get("file_path") or (block.get("input") or {}).get("notebook_path") or ""
                    edited.add(p.replace("\\", "/"))
    if not usage:
        return None
    stamps.sort()
    active = sum(((b - a) for a, b in zip(stamps, stamps[1:]) if b - a <= IDLE_GAP), timedelta())
    meta_path = path.with_suffix(".meta.json")
    meta = json.loads(meta_path.read_text(encoding="utf-8")) if meta_path.exists() else {}
    models = {m for _, m in usage.values() if m != "<synthetic>"}
    return {
        "session": path.parent.parent.name[:8],
        "agent": path.stem.removeprefix("agent-")[:10],
        "start": stamps[0],
        "type": meta.get("agentType", "?"),
        "desc": meta.get("description", ""),
        "model": ",".join(sorted(m.removeprefix("claude-") for m in models)) or "?",
        "steps": len(usage),
        "minutes": active.total_seconds() / 60,
        "read": sum(u.get("cache_read_input_tokens", 0) for u, _ in usage.values()),
        "cwrite": sum(u.get("cache_creation_input_tokens", 0) for u, _ in usage.values()),
        "out": sum(u.get("output_tokens", 0) for u, _ in usage.values()),
        "code": any(p.endswith(CODE_EXT) for p in edited),
        "ui": any(is_ui_path(p) for p in edited),
    }


def median_line(label, runs):
    if not runs:
        return f"{label}: brak przebiegów"
    med = lambda k: statistics.median(r[k] for r in runs)
    return (f"{label}: {len(runs)} przebiegów, mediana {med('steps'):.0f} kroków, "
            f"{med('minutes'):.1f} min, {med('read') / 1e6:.1f} mln odczytu; "
            f"suma odczytu {sum(r['read'] for r in runs) / 1e6:.0f} mln")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--since", help="data RRRR-MM-DD (UTC), włącznie")
    ap.add_argument("--until", help="data RRRR-MM-DD (UTC), włącznie")
    ap.add_argument("--session", help="przedrostek identyfikatora sesji architekta")
    ap.add_argument("--ui", action="store_true", help="tylko przebiegi dotykające interfejsu")
    ap.add_argument("--top", type=int, default=0, help="pokaż tylko N najdroższych (po odczycie)")
    args = ap.parse_args()

    runs = [r for p in sorted(ROOT.glob("*/subagents/*.jsonl")) if (r := read_run(p))]
    if args.since:
        runs = [r for r in runs if r["start"].date().isoformat() >= args.since]
    if args.until:
        runs = [r for r in runs if r["start"].date().isoformat() <= args.until]
    if args.session:
        runs = [r for r in runs if r["session"].startswith(args.session[:8])]
    if args.ui:
        runs = [r for r in runs if r["ui"]]

    shown = sorted(runs, key=lambda r: -r["read"])[: args.top] if args.top else sorted(runs, key=lambda r: r["start"])
    print(f"{'start (UTC)':16} {'sesja':8} {'rodzaj':10} {'model':10} {'kroki':>5} {'min':>5} "
          f"{'odczyt M':>8} {'zapis k':>7} {'kod':3} {'UI':2}  opis")
    for r in shown:
        print(f"{r['start']:%Y-%m-%d %H:%M} {r['session']:8} {r['type'][:10]:10} {r['model'][:10]:10} "
              f"{r['steps']:5} {r['minutes']:5.1f} {r['read'] / 1e6:8.1f} {r['out'] / 1e3:7.1f} "
              f"{'tak' if r['code'] else '':3} {'tak' if r['ui'] else '':2}  {r['desc'][:60]}")
    print()
    print(median_line("wszystkie", runs))
    print(median_line("z kodem", [r for r in runs if r["code"]]))
    print(median_line("interfejs", [r for r in runs if r["ui"]]))


if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
