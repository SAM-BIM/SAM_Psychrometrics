"""The SAM-BIM Grasshopper component metadata contract for this repository.

Reads the component .cs files (the same scanner as `inventory.py`, SAM-BIM/SAM#166) and extracts the
published parameter contract of every non-abstract component that declares a `ComponentGuid`:
the component name/nickname/category/subcategory, `LatestComponentVersion`, and for every declared
input and output the name, nickname, description, `Optional`, `Access`, `ParamVisibility` and any
persistent default.

  python param_contract.py            # (re)write param_contract.json from source
  python param_contract.py --check    # verify source against the committed param_contract.json

The contract exists because Grasshopper restores a component from the parameter archive stored in a
definition: a reopened component keeps the names the FILE has, not the names its class now declares.
Renaming a parameter therefore breaks every `Params.IndexOfInputParam`/`IndexOfOutputParam` lookup in
every existing definition, silently. The manifest pins the current names, and `--check` fails if any of
them, or the order, access, optionality, visibility or GUID, changes.

Naming-standard deviations (camelCase outputs, `_name_` inputs without a persistent default) are the
FROZEN legacy exceptions listed in `namingExceptions`: they are expected, not errors, but a NEW deviation,
or a listed exception that no longer deviates, fails the check so the list stays honest. Descriptions are
not frozen - they are checked for quality only.
"""
import json
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

import inventory  # reuse the repository / Grasshopper-project scanner

DESIGN = os.path.dirname(HERE)
CONTRACT = os.path.join(DESIGN, "param_contract.json")

SPDX = ("// SPDX-License-Identifier: LGPL-3.0-or-later",
        "// Copyright (c) 2020-2026 Michal Dengusiak & Jakub Ziolkowski and contributors")


# --- C# lexing helpers ------------------------------------------------------------------------------

def take_balanced(text, index):
    """Return the text between text[index] ('(' or '{') and its matching closer, and the index after it."""
    opener = text[index]
    closer = ")" if opener == "(" else "}"
    depth, i, instr, esc = 0, index, False, False
    while i < len(text):
        ch = text[i]
        if instr:
            if esc:
                esc = False
            elif ch == "\\":
                esc = True
            elif ch == '"':
                instr = False
        elif ch == '"':
            instr = True
        elif ch == opener:
            depth += 1
        elif ch == closer:
            depth -= 1
            if depth == 0:
                return text[index + 1:i], i + 1
        i += 1
    raise ValueError("unbalanced %s at %d" % (opener, index))


def split_top_level(text):
    parts, depth, cur, instr, esc = [], 0, "", False, False
    for ch in text:
        if instr:
            cur += ch
            if esc:
                esc = False
            elif ch == "\\":
                esc = True
            elif ch == '"':
                instr = False
        elif ch == '"':
            instr = True
            cur += ch
        elif ch in "([{":
            depth += 1
            cur += ch
        elif ch in ")]}":
            depth -= 1
            cur += ch
        elif ch == "," and depth == 0:
            parts.append(cur)
            cur = ""
        else:
            cur += ch
    parts.append(cur)
    return parts


def parse_initializer(body):
    out = {}
    for part in split_top_level(body):
        part = part.strip()
        if not part or "=" not in part:
            continue
        key, value = part.split("=", 1)
        out[key.strip()] = value.strip()
    return out


def unquote(value):
    value = value.strip()
    if len(value) >= 2 and value[0] == '"' and value[-1] == '"':
        value = value[1:-1]
    return value.replace('\\"', '"').replace("\\n", "\n").replace("\\\\", "\\")


def param_type(expression):
    match = re.search(r"Param_\w+", expression)
    return match.group(0) if match else None


def parse_params(block):
    """Every `result.Add(new GH_SAMParam(...))` in a declaration block, in source order."""
    variables = {}
    for match in re.finditer(r"(\w+)\s*=\s*new\s+[\w:.]*Param_\w+\(\)\s*\{", block):
        body, _ = take_balanced(block, block.index("{", match.start()))
        variables[match.group(1)] = {"init": parse_initializer(body), "type": param_type(block[match.start():match.end()])}
    for match in re.finditer(r"(\w+)\.SetPersistentData\(([^;]*)\)", block):
        if match.group(1) in variables:
            variables[match.group(1)]["persistentDefault"] = match.group(2).strip()

    result = []
    for match in re.finditer(r"result\.Add\(new GH_SAMParam\(", block):
        args, _ = take_balanced(block, match.end() - 1)
        parts = split_top_level(args)
        first = parts[0].strip()
        vis = parts[1].strip().replace("ParamVisibility.", "") if len(parts) > 1 else None

        if first.startswith("new "):
            body, _ = take_balanced(block, block.index("{", match.end()))
            init = parse_initializer(body)
            ptype = param_type(first[:first.index("{")])
            persistent = None
        else:
            entry = variables.get(first, {})
            init = entry.get("init", {})
            ptype = entry.get("type")
            persistent = entry.get("persistentDefault")

        result.append({
            "name": unquote(init.get("Name", "")),
            "nickname": unquote(init.get("NickName", "")),
            "description": unquote(init.get("Description", "")),
            "type": ptype,
            "optional": init.get("Optional", "false").strip() == "true",
            "access": (init.get("Access", "") or "").split(".")[-1] or None,
            "visibility": vis,
            "persistentDefault": persistent,
        })
    return result


def components():
    """The component metadata for this repository, in a stable order."""
    items = []
    for project_dir, project in sorted(inventory.gh_projects().items(), key=lambda kv: kv[1]):
        removed = inventory.compile_removed(project_dir)
        for root, dirs, files in os.walk(project_dir):
            dirs[:] = sorted(d for d in dirs if d not in inventory.SKIP_DIRS)
            for f in sorted(files):
                if not f.endswith(".cs") or f.endswith(".Designer.cs"):
                    continue
                path = os.path.join(root, f)
                if removed(os.path.relpath(path, project_dir)):
                    continue
                source = open(inventory.long_path(path), encoding="utf-8-sig").read()
                if "ComponentGuid" not in source:
                    continue
                code = re.sub(r"(?m)(^|[\s;{}])//[^\n]*", r"\1", source)
                classes = list(inventory.CLASS_RE.finditer(code))
                for i, class_match in enumerate(classes):
                    body = code[class_match.end(): classes[i + 1].start() if i + 1 < len(classes) else len(code)]
                    guid = inventory.GUID_RE.search(body)
                    if not guid or "abstract" in class_match.group(1):
                        continue
                    ctor = re.search(r"\b" + class_match.group(2) + r"\s*\(\s*\)\s*(?=:\s*base\()", body)
                    base = inventory.base_args(body, ctor.end()) if ctor else None
                    if base and base[-1].startswith("GH_ParamAccess"):
                        base = base[:-1]

                    version = re.search(r"LatestComponentVersion\s*=>\s*\"([^\"]+)\"", body)
                    inputs_match = re.search(r"GH_SAMParam\[\]\s+Inputs", body)
                    outputs_match = re.search(r"GH_SAMParam\[\]\s+Outputs", body)
                    inputs = parse_params(body[inputs_match.start():outputs_match.start()]) if inputs_match and outputs_match else []
                    outputs = parse_params(body[outputs_match.start():]) if outputs_match else []

                    items.append({
                        "class": class_match.group(2),
                        "guid": guid.group(1).lower(),
                        "name": unquote(inventory.lit(base[0])) if base else None,
                        "nickname": unquote(inventory.lit(base[1])) if base and len(base) > 1 else None,
                        "category": unquote(inventory.lit(base[-2])) if base and len(base) >= 4 else None,
                        "subcategory": unquote(inventory.lit(base[-1])) if base and len(base) >= 4 else None,
                        "description": unquote(inventory.lit(base[2])) if base and len(base) >= 4 else None,
                        "latestComponentVersion": version.group(1) if version else None,
                        "inputs": inputs,
                        "outputs": outputs,
                        "source": os.path.relpath(path, inventory.REPO).replace("\\", "/"),
                    })
    return sorted(items, key=lambda item: item["class"])


# --- naming standard and its frozen legacy exceptions ------------------------------------------------

def base_name(name):
    return name.strip("_")


def expected_name(name, side, optional, persistent_default):
    core = base_name(name)
    if side == "output":
        return core[:1].upper() + core[1:]
    if not optional:
        return "_" + core
    return ("_" + core + "_") if persistent_default is not None else (core + "_")


def deviations(contract):
    """Client identifiers whose declared name does not follow the SAM-BIM naming standard."""
    result = []
    for component in contract["components"]:
        for side in ("inputs", "outputs"):
            for param in component[side]:
                expected = expected_name(param["name"], side[:-1], param["optional"], param["persistentDefault"])
                if param["name"] != expected:
                    result.append("%s.%s.%s" % (component["class"], side[:-1], param["name"]))
    return result


def quality_problems(contract):
    problems = []
    for component in contract["components"]:
        if not component["latestComponentVersion"]:
            problems.append("%s has no LatestComponentVersion" % component["class"])
        for side in ("inputs", "outputs"):
            for param in component[side]:
                where = "%s.%s.%s" % (component["class"], side[:-1], param["name"])
                description = (param["description"] or "").strip()
                if not description:
                    problems.append("%s: empty description" % where)
                elif not description[0].isupper():
                    problems.append("%s: description does not begin with a capital letter" % where)
                if "Humidty" in description:
                    problems.append("%s: known typo 'Humidty'" % where)
                if param["nickname"] != param["name"]:
                    problems.append("%s: NickName '%s' != Name" % (where, param["nickname"]))
                if param["access"] != "item":
                    problems.append("%s: unexpected access '%s'" % (where, param["access"]))
    return problems


# --- entry point ------------------------------------------------------------------------------------

def diff(committed, current):
    problems = []
    by_class = {c["class"]: c for c in committed["components"]}
    for component in current["components"]:
        previous = by_class.pop(component["class"], None)
        if previous is None:
            problems.append("new component %s (%s)" % (component["class"], component["guid"]))
            continue
        for field in ("guid", "name", "nickname", "category", "subcategory", "latestComponentVersion"):
            if previous[field] != component[field]:
                problems.append("%s.%s: %r -> %r" % (component["class"], field, previous[field], component[field]))
        for side in ("inputs", "outputs"):
            old = [(p["name"], p["nickname"], p["description"], p["optional"], p["access"], p["visibility"], p["persistentDefault"]) for p in previous[side]]
            new = [(p["name"], p["nickname"], p["description"], p["optional"], p["access"], p["visibility"], p["persistentDefault"]) for p in component[side]]
            if [x[0] for x in old] != [x[0] for x in new]:
                problems.append("%s.%s: names/order changed\n    committed: %s\n    current:   %s"
                                % (component["class"], side, [x[0] for x in old], [x[0] for x in new]))
                continue
            for a, b in zip(old, new):
                # name, nickname, optional, access, visibility and persistent default are frozen contract
                for index, label in ((1, "nickname"), (3, "optional"), (4, "access"), (5, "visibility"), (6, "persistentDefault")):
                    if a[index] != b[index]:
                        problems.append("%s.%s.%s: %s %r -> %r" % (component["class"], side, a[0], label, a[index], b[index]))
    for leftover in by_class:
        problems.append("component removed: %s" % leftover)
    return problems


def main(argv):
    current = {
        "repository": os.path.basename(inventory.REPO),
        "components": components(),
        "namingExceptions": sorted(deviations({"components": components()})),
    }
    if len(argv) > 1 and argv[1] == "--check":
        committed = json.load(open(CONTRACT, encoding="utf-8"))
        problems = diff(committed, current)
        # naming standard: expected deviations are the documented legacy exceptions, nothing more
        expected_exceptions = sorted(committed.get("namingExceptions", []))
        actual_exceptions = sorted(deviations(current))
        if expected_exceptions != actual_exceptions:
            problems.append("naming exceptions changed (a new deviation, or a stale exception):\n"
                            "    documented: %s\n    actual:     %s" % (expected_exceptions, actual_exceptions))
        problems += quality_problems(current)
        for problem in problems:
            print("FAIL", problem)
        print("%d components checked: %s" % (len(current["components"]), "OK" if not problems else "%d PROBLEM(S)" % len(problems)))
        return 1 if problems else 0

    with open(CONTRACT, "w", encoding="utf-8", newline="\n") as stream:
        json.dump(current, stream, indent=1)
        stream.write("\n")
    print("%d components -> %s" % (len(current["components"]), CONTRACT))
    print("%d documented naming exceptions" % len(current["namingExceptions"]))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
