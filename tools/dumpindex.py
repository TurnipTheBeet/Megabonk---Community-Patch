#!/usr/bin/env python3
"""
dumpindex - fast lookup tool for the Megabonk il2cpp dump.

The dump (dump.cs) is a ~700k-line skeleton: empty method bodies, with the
useful data (field offsets, method addresses, enum values, namespaces) living
in comments and enum declarations. Standard code tools throw comments away,
so this purpose-built parser reads exactly that data into a JSON index and
gives instant exact lookups - the "phone book" that tells Ghidra where to aim.

Usage:
    python dumpindex.py build                 # parse dump.cs -> index.json
    python dumpindex.py type   <Name>         # full type: namespace, fields, methods, enum members
    python dumpindex.py field  <Name>         # find field(s) by name -> offset(s)
    python dumpindex.py method <Name>         # find method(s) by name -> VA / RVA / Offset
    python dumpindex.py enum   <Name>         # enum members + values (or a member name -> value)
    python dumpindex.py find   <substring>    # fuzzy-search type names
    python dumpindex.py addr   <hexVA>        # reverse: VA / FUN_xxx / 0x... -> Type.Method

The index auto-builds on first lookup if index.json is missing.
Override the dump location with  --dump <path>  on any command.
"""

import json
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
DEFAULT_DUMP = Path(r"C:\Users\kenne\Desktop\il2cppdumper\megabonk\dump.cs")
INDEX_PATH = HERE / "index.json"

# A real type declaration line always carries a "// TypeDefIndex:" comment.
RE_TYPEDEF = re.compile(r"//\s*TypeDefIndex:\s*(\d+)")
RE_KIND_NAME = re.compile(r"\b(class|struct|enum|interface)\s+([^\s:{]+)")
RE_NAMESPACE = re.compile(r"^//\s*Namespace:\s*(.*?)\s*$")
# Field: "<modifiers/type> <name>; // 0xNN"
RE_FIELD = re.compile(r"^\s*(.+?)\s+([^\s;]+);\s*//\s*(0x[0-9A-Fa-f]+)\s*$")
# Method address comment: "// RVA: 0x.. Offset: 0x.. VA: 0x.."
RE_ADDR = re.compile(
    r"//\s*RVA:\s*(0x[0-9A-Fa-f]+)"
    r"(?:\s*Offset:\s*(0x[0-9A-Fa-f]+))?"
    r"(?:\s*VA:\s*(0x[0-9A-Fa-f]+))?"
)
# Enum member: "public const <EnumType> <Name> = <value>;"
RE_ENUM_MEMBER = re.compile(r"\bconst\s+\S+\s+([A-Za-z_]\w*)\s*=\s*([^;]+);")
# Method signature: a line with (...) ending in a body, capture the name before '('
RE_METHOD_SIG = re.compile(r"([A-Za-z_][\w.<>]*)\s*\(")


def build(dump_path: Path) -> dict:
    if not dump_path.exists():
        sys.exit(f"dump not found: {dump_path}")

    types = {}            # name -> list of type records (names can collide)
    namespace = ""        # current namespace
    cur = None            # current type record
    pending_addr = None   # (rva, offset, va) waiting for the next signature line

    with dump_path.open(encoding="utf-8", errors="replace") as fh:
        for raw in fh:
            line = raw.rstrip("\n")
            stripped = line.strip()

            m = RE_NAMESPACE.match(stripped)
            if m:
                namespace = m.group(1)
                continue

            if RE_TYPEDEF.search(line):
                km = RE_KIND_NAME.search(line)
                if km:
                    kind, name = km.group(1), km.group(2)
                    cur = {
                        "name": name,
                        "kind": kind,
                        "namespace": namespace,
                        "typedef": int(RE_TYPEDEF.search(line).group(1)),
                        "fields": [],
                        "methods": [],
                        "members": [],   # enum members
                    }
                    types.setdefault(name, []).append(cur)
                    pending_addr = None
                continue

            if cur is None:
                continue

            # Enum members
            if cur["kind"] == "enum":
                em = RE_ENUM_MEMBER.search(stripped)
                if em and em.group(1) != "value__":
                    cur["members"].append({"name": em.group(1), "value": em.group(2).strip()})
                continue

            # Method address comment -> remember until we hit the signature
            am = RE_ADDR.search(stripped)
            if am and stripped.startswith("//"):
                pending_addr = (am.group(1), am.group(2), am.group(3))
                continue

            # Field: "<type> <name>; // 0xNN"
            fm = RE_FIELD.match(line)
            if fm and "(" not in fm.group(1):
                cur["fields"].append({
                    "name": fm.group(2),
                    "type": fm.group(1).strip(),
                    "offset": fm.group(3),
                })
                continue

            # Method signature following an address comment
            if pending_addr and "(" in stripped and not stripped.startswith("//"):
                sm = RE_METHOD_SIG.search(stripped)
                if sm:
                    name = sm.group(1).split(".")[-1]
                    rva, off, va = pending_addr
                    cur["methods"].append({
                        "name": name,
                        "signature": stripped.replace(" { }", "").strip(),
                        "va": va, "rva": rva, "offset": off,
                    })
                pending_addr = None
                continue

    n_types = sum(len(v) for v in types.values())
    n_fields = sum(len(t["fields"]) for v in types.values() for t in v)
    n_methods = sum(len(t["methods"]) for v in types.values() for t in v)
    index = {
        "dump": str(dump_path),
        "stats": {"types": n_types, "fields": n_fields, "methods": n_methods},
        "types": types,
    }
    INDEX_PATH.write_text(json.dumps(index, ensure_ascii=False), encoding="utf-8")
    return index


def load(dump_path: Path) -> dict:
    if not INDEX_PATH.exists():
        return build(dump_path)
    return json.loads(INDEX_PATH.read_text(encoding="utf-8"))


def _iter_types(index):
    for recs in index["types"].values():
        for t in recs:
            yield t


def cmd_type(index, name):
    recs = index["types"].get(name)
    if not recs:
        # try case-insensitive
        recs = [t for t in _iter_types(index) if t["name"].lower() == name.lower()]
    if not recs:
        print(f"no type named '{name}'. try:  find {name}")
        return
    for t in recs:
        ns = t["namespace"] or "(global)"
        print(f"\n{t['kind']} {t['name']}   namespace: {ns}   TypeDefIndex: {t['typedef']}")
        if t["members"]:
            print("  enum members:")
            for mb in t["members"]:
                print(f"    {mb['name']} = {mb['value']}")
        if t["fields"]:
            print("  fields:")
            for f in t["fields"]:
                print(f"    {f['offset']:<8} {f['name']}   ({f['type']})")
        if t["methods"]:
            print("  methods:")
            for mth in t["methods"]:
                va = mth["va"] or "-"
                print(f"    VA {va:<14} {mth['name']}   {mth['signature']}")


def cmd_field(index, name):
    hits = []
    for t in _iter_types(index):
        for f in t["fields"]:
            if f["name"].lower() == name.lower():
                hits.append((t, f))
    if not hits:
        # substring fallback
        for t in _iter_types(index):
            for f in t["fields"]:
                if name.lower() in f["name"].lower():
                    hits.append((t, f))
    if not hits:
        print(f"no field matching '{name}'")
        return
    for t, f in hits:
        print(f"{f['offset']:<8} {t['name']}.{f['name']}   ({f['type']})")


def cmd_method(index, name):
    hits = []
    for t in _iter_types(index):
        for m in t["methods"]:
            if m["name"].lower() == name.lower():
                hits.append((t, m))
    if not hits:
        for t in _iter_types(index):
            for m in t["methods"]:
                if name.lower() in m["name"].lower():
                    hits.append((t, m))
    if not hits:
        print(f"no method matching '{name}'")
        return
    for t, m in hits:
        va = m["va"] or "-"
        rva = m["rva"] or "-"
        print(f"VA {va:<14} RVA {rva:<12} {t['name']}.{m['name']}   {m['signature']}")


def cmd_enum(index, name):
    # exact enum type?
    recs = [t for t in _iter_types(index) if t["kind"] == "enum" and t["name"].lower() == name.lower()]
    if recs:
        for t in recs:
            print(f"\nenum {t['name']}   namespace: {t['namespace'] or '(global)'}")
            for mb in t["members"]:
                print(f"    {mb['name']} = {mb['value']}")
        return
    # otherwise treat as a member name across all enums
    hits = []
    for t in _iter_types(index):
        if t["kind"] != "enum":
            continue
        for mb in t["members"]:
            if mb["name"].lower() == name.lower():
                hits.append((t, mb))
    if not hits:
        print(f"no enum or enum-member matching '{name}'")
        return
    for t, mb in hits:
        print(f"{t['name']}.{mb['name']} = {mb['value']}")


def _norm_va(s):
    # accept "FUN_1803e2820", "0x1803E2820", "1803e2820" -> "0x1803e2820"
    s = s.strip().lower()
    for p in ("fun_", "lab_", "dat_", "_dat_", "0x"):
        if s.startswith(p):
            s = s[len(p):]
    return "0x" + s.lstrip("0").rjust(1, "0") if s else s


def cmd_addr(index, raw):
    target = _norm_va(raw)
    hits = []
    for t in _iter_types(index):
        for m in t["methods"]:
            if m["va"] and _norm_va(m["va"]) == target:
                hits.append((t, m))
    if not hits:
        print(f"no method at VA {target} (raw '{raw}')")
        return
    for t, m in hits:
        print(f"{m['va']}  {t['name']}.{m['name']}   {m['signature']}")


def cmd_find(index, sub):
    hits = sorted({t["name"] for t in _iter_types(index) if sub.lower() in t["name"].lower()})
    if not hits:
        print(f"no type name contains '{sub}'")
        return
    for h in hits[:60]:
        print(h)
    if len(hits) > 60:
        print(f"... ({len(hits)} total)")


def main():
    args = sys.argv[1:]
    dump_path = DEFAULT_DUMP
    if "--dump" in args:
        i = args.index("--dump")
        dump_path = Path(args[i + 1])
        del args[i:i + 2]
    if not args:
        print(__doc__)
        return

    cmd = args[0]
    rest = args[1:]

    if cmd == "build":
        idx = build(dump_path)
        s = idx["stats"]
        print(f"built index.json - {s['types']} types, {s['fields']} fields, {s['methods']} methods")
        return

    index = load(dump_path)
    if cmd == "type" and rest:
        cmd_type(index, rest[0])
    elif cmd == "field" and rest:
        cmd_field(index, rest[0])
    elif cmd == "method" and rest:
        cmd_method(index, rest[0])
    elif cmd == "enum" and rest:
        cmd_enum(index, rest[0])
    elif cmd == "find" and rest:
        cmd_find(index, rest[0])
    elif cmd == "addr" and rest:
        cmd_addr(index, rest[0])
    else:
        print(__doc__)


if __name__ == "__main__":
    main()
