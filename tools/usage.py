"""Token use and cost of Claude Code sessions in this project, read from the local transcripts.

Every agent step resends the whole context, so the cost of a session is driven by its number of
steps times its context size; this prints both for each architect session and its executors.

Usage:
  python tools/usage.py                      # sessions from the last 14 days
  python tools/usage.py --since 2026-10-01   # sessions started on or after the day (UTC)
  python tools/usage.py --session c6993e7e   # one session, each executor on its own line

Definitions:
  step    - one model call (a unique message id; a reply spans several transcript lines),
  context - input + cache read + cache write tokens of a step; "peak" is the largest,
  cost    - USD at Anthropic API list prices (PRICES below), an estimate of what a session takes
            from a subscription's limits, not a bill.
Resumed and forked sessions copy earlier messages into a new file; each message is counted once.
"""

import argparse
import datetime
import glob
import json
import os
import sys

# USD per million tokens: input, output, cache write (5 min), cache write (1 h), cache read.
PRICES = {
    "claude-opus-5-5": (4, 20, 5, 8, 0.20),
    "claude-opus-5": (5, 25, 6.25, 10, 0.50),
    "claude-sonnet-5-5": (2, 10, 2.5, 4, 0.20),
    "claude-sonnet-5": (2, 10, 2.5, 4, 0.20),
    "claude-haiku-4-5-20251001": (1, 5, 1.25, 2, 0.10),
}


def project_dir():
    repo = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    name = repo.replace(":", "-").replace("\\", "-").replace("/", "-")
    return os.path.join(os.path.expanduser("~"), ".claude", "projects", name)


def read_steps(path):
    """Unique steps of one transcript: message id -> (timestamp, model, usage)."""
    steps = {}
    with open(path, encoding="utf-8") as transcript:
        for line in transcript:
            try:
                entry = json.loads(line)
            except json.JSONDecodeError:
                continue
            if entry.get("type") != "assistant":
                continue
            message = entry.get("message") or {}
            usage = message.get("usage") or {}
            known = steps.get(message.get("id"))
            # Each content block repeats the usage; the last line carries the final output count.
            if known is None or (usage.get("output_tokens") or 0) >= (known[2].get("output_tokens") or 0):
                steps[message.get("id")] = (entry.get("timestamp") or "", message.get("model"), usage)
    return steps


def step_cost(model, usage):
    prices = PRICES.get(model)
    if prices is None:
        return 0.0
    creation = usage.get("cache_creation") or {}
    short = creation.get("ephemeral_5m_input_tokens")
    long = creation.get("ephemeral_1h_input_tokens") or 0
    if short is None:
        short = usage.get("cache_creation_input_tokens") or 0
    return ((usage.get("input_tokens") or 0) * prices[0] + (usage.get("output_tokens") or 0) * prices[1]
            + short * prices[2] + long * prices[3] + (usage.get("cache_read_input_tokens") or 0) * prices[4]) / 1e6


def context(usage):
    return ((usage.get("input_tokens") or 0) + (usage.get("cache_read_input_tokens") or 0)
            + (usage.get("cache_creation_input_tokens") or 0))


class Tally:
    def __init__(self, steps, seen):
        fresh = [value for key, value in steps.items() if key not in seen]
        seen.update(steps)
        self.steps = len(fresh)
        self.cost = sum(step_cost(model, usage) for _, model, usage in fresh)
        self.peak = max((context(usage) for _, _, usage in fresh), default=0)
        stamps = sorted(stamp for stamp, _, _ in fresh if stamp)
        self.start = stamps[0] if stamps else ""
        models = sorted({model for _, model, _ in fresh if model in PRICES})
        self.models = ", ".join(model.replace("claude-", "") for model in models)


def load_json(path, key):
    try:
        with open(path, encoding="utf-8") as file:
            return json.load(file).get(key)
    except (OSError, json.JSONDecodeError):
        return None


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--since", help="first day, YYYY-MM-DD (UTC); default: 14 days ago")
    parser.add_argument("--session", help="prefix of one session id: list its executors")
    parser.add_argument("--project-dir", default=project_dir(), help="transcripts directory")
    args = parser.parse_args()
    since = args.since or (datetime.datetime.now(datetime.timezone.utc) - datetime.timedelta(days=14)).strftime("%Y-%m-%d")
    root = args.project_dir
    if not os.path.isdir(root):
        sys.exit(f"Brak katalogu z zapisami sesji: {root}")

    seen = set()
    rows = []
    # Oldest first, so a resumed session's copied messages count for the session that made them.
    for path in sorted(glob.glob(os.path.join(root, "*.jsonl")), key=os.path.getmtime):
        session = os.path.basename(path)[:-6]
        main_tally = Tally(read_steps(path), seen)
        executors = []
        for sub in sorted(glob.glob(os.path.join(root, session, "subagents", "*.jsonl"))):
            tally = Tally(read_steps(sub), seen)
            tally.name = load_json(sub[:-6] + ".meta.json", "description") or os.path.basename(sub)
            executors.append(tally)
        start = main_tally.start or min((e.start for e in executors if e.start), default="")
        if not start or (main_tally.steps == 0 and not executors):
            continue
        if args.session:
            if not session.startswith(args.session):
                continue
        elif start[:10] < since:
            continue
        title = load_json(os.path.join(root, session, "custom-title.json"), "customTitle") or ""
        rows.append((start, session, title, main_tally, executors))

    rows.sort(key=lambda row: row[0])
    print(f"{'start (UTC)':16} {'sesja':8}  {'architekt: kroki':>16} {'szczyt':>7} {'koszt':>8}"
          f"  {'wykonawcy: n':>12} {'kroki':>6} {'koszt':>8}  tytuł")
    total_main = total_executors = 0.0
    for start, session, title, main_tally, executors in rows:
        executor_steps = sum(e.steps for e in executors)
        executor_cost = sum(e.cost for e in executors)
        total_main += main_tally.cost
        total_executors += executor_cost
        print(f"{start[:16].replace('T', ' '):16} {session[:8]:8}  {main_tally.steps:16} {main_tally.peak // 1000:6}k"
              f" {main_tally.cost:7.2f}$  {len(executors):12} {executor_steps:6} {executor_cost:7.2f}$  {title}")
        if args.session:
            for e in executors:
                print(f"{'':27}{e.steps:5} kroków, szczyt {e.peak // 1000:3}k, {e.cost:6.2f}$  {e.models:12} {e.name}")
    print(f"razem: architekt {total_main:.2f}$, wykonawcy {total_executors:.2f}$; sesje: {len(rows)}")


if __name__ == "__main__":
    main()
