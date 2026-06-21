#!/usr/bin/env python3
"""Compare current Roslyn code metrics against the committed baseline and report
regressions. Advisory by default (exit 0); pass --strict to fail on regression.
Usage: metrics-ratchet.py <baseline.json> <current.json> [--strict]"""
import json, sys

MI_DROP_MAX = 5      # a touched file may not lose more than this much Maintainability Index
NEW_MI_MIN  = 40     # a brand-new file must clear this MI
NEW_CC_MAX  = 250    # a brand-new file's summed cyclomatic complexity cap

def by_file(doc): return {f["file"]: f for f in doc["files"]}

def main():
    if len(sys.argv) < 3:
        print("usage: metrics-ratchet.py <baseline.json> <current.json> [--strict]"); return 2
    base = by_file(json.load(open(sys.argv[1])))
    cur  = by_file(json.load(open(sys.argv[2])))
    strict = "--strict" in sys.argv
    regress, newbad = [], []
    for path, c in sorted(cur.items()):
        b = base.get(path)
        if b is None:
            if c["mi"] < NEW_MI_MIN:   newbad.append((path, f"new file: MI {c['mi']} < {NEW_MI_MIN}"))
            if c["cc"] > NEW_CC_MAX:   newbad.append((path, f"new file: cyclomatic {c['cc']} > {NEW_CC_MAX}"))
            continue
        if b["mi"] - c["mi"] > MI_DROP_MAX:
            regress.append((path, f"MI {b['mi']} -> {c['mi']}  (-{b['mi']-c['mi']})"))
        if c["couplingMax"] > b["couplingMax"]:
            regress.append((path, f"class coupling {b['couplingMax']} -> {c['couplingMax']}"))
    n = len(regress) + len(newbad)
    if n == 0:
        print(f"metrics ratchet: OK ({len(cur)} files, no regression vs baseline)"); return 0
    print(f"metrics ratchet: {n} issue(s) vs baseline "
          f"[{'BLOCKING' if strict else 'advisory'}]")
    for path, msg in regress: print(f"  REGRESSION  {path}: {msg}")
    for path, msg in newbad:  print(f"  NEW-FILE    {path}: {msg}")
    print("  (regenerate the baseline with `bash scripts/metrics-refresh.sh` once the change is intended.)")
    return 1 if strict else 0

sys.exit(main())
