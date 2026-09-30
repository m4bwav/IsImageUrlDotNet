"""Replays IsImageUrlDotNet's golden capture (tests/Golden/Capture) against the published 1.0.2 and 2.0.0.

Read-only on the repository: it copies the capture program into scratch, changes only the package version in the
copy for 2.0.0 (the capture pins [1.0.2] in Capture.fsproj and has no property for it), builds each copy once, runs
each target framework as a child process one after the other (the capture starts its own fixture server, which is
also every request's proxy), and compares each run with the committed recording, answers and requests apart.
It never writes to tests/Golden.

    python golden-replay.py <repository clone> <scratch folder>

Saved as ai-docs/notes/2026-09-29-golden-replay.py, its output as 2026-09-29-golden-replay.out.txt.
"""
import json
import os
import pathlib
import platform
import re
import shutil
import subprocess
import sys

REPO = pathlib.Path(sys.argv[1]).resolve()
SCRATCH = pathlib.Path(sys.argv[2]).resolve()
GOLDEN = REPO / "tests" / "Golden"
WINDOWS = platform.system() == "Windows"
OS = "windows" if WINDOWS else ("macos" if platform.system() == "Darwin" else "linux")
TFMS = ["net10.0", "net48"] if WINDOWS else ["net10.0"]
ENV = {k: v for k, v in os.environ.items() if k.upper() not in ("HTTP_PROXY", "HTTPS_PROXY", "ALL_PROXY", "NO_PROXY")}


def mask(text):
    return text.replace(str(SCRATCH), "<scratch>").replace(str(REPO), "<repo>").replace(str(pathlib.Path.home()), "<home>")


def run(args, cwd):
    p = subprocess.run(args, cwd=cwd, env=ENV, capture_output=True, text=True, encoding="utf-8")
    return p.returncode, p.stdout, p.stderr


def recording(tfm):
    name = f"1.0.2.{'net48' if tfm == 'net48' else 'net10.0'}-{OS}.json"
    return name, json.loads((GOLDEN / name).read_text(encoding="utf-8"))


def drive_neutral(value):
    # file:///nonexistent-isimageurl/file resolves against the current drive on Windows; the recordings were made on
    # D: (the golden test's one exception table entry does the same swap).
    text = json.dumps(value, ensure_ascii=False, sort_keys=True)
    return re.sub(r"'[A-Z]:\\\\nonexistent-isimageurl", "'D:\\\\\\\\nonexistent-isimageurl", text)


def compare(expected, actual):
    """Counts cases whose answer (result) and whose requests match, and lists the differing ones."""
    same_answer = same_requests = same_both = drive_only = 0
    differing = []
    for i, (e, a) in enumerate(zip(expected, actual)):
        if (e["method"], e["args"], e["culture"]) != (a["method"], a["args"], a["culture"]):
            differing.append(f"case {i}: misaligned ({e['method']} {e['args']} vs {a['method']} {a['args']})")
            continue
        ans = e["result"] == a["result"]
        if not ans and drive_neutral(e["result"]) == drive_neutral(a["result"]):
            ans = True
            drive_only += 1
        req = e["requests"] == a["requests"]
        same_answer += ans
        same_requests += req
        same_both += ans and req
        if not (ans and req):
            differing.append(f"case {i} {e['method']} {json.dumps(e['args'], ensure_ascii=False)}: "
                             f"{'answer differs' if not ans else ''}{' and ' if not ans and not req else ''}{'requests differ' if not req else ''}\n"
                             f"    recorded: {json.dumps(e['result'], ensure_ascii=False)} {json.dumps(e['requests'], ensure_ascii=False)}\n"
                             f"    today:    {json.dumps(a['result'], ensure_ascii=False)} {json.dumps(a['requests'], ensure_ascii=False)}")
    return same_answer, same_requests, same_both, drive_only, differing


def template_view(case):
    """What package-modernize's golden-capture-nuget template records per case: method, args, result, where a throw is
    {"$throws": "Type: message"} with no WebException status, no inner exception, no culture and no requests."""
    result = case["result"]
    if isinstance(result, dict) and "$throws" in result:
        result = {"$throws": result["$throws"]}
    return case["method"], json.dumps(case["args"]), json.dumps(result, sort_keys=True)


print(f"## golden replay on {OS}: {', '.join(TFMS)}")
runs = {}
for version in ["1.0.2", "2.0.0"]:
    copy = SCRATCH / f"capture-{version}"
    if copy.exists():
        shutil.rmtree(copy)
    shutil.copytree(GOLDEN / "Capture", copy, ignore=shutil.ignore_patterns("bin", "obj"))
    fsproj = copy / "Capture.fsproj"
    text = fsproj.read_text(encoding="utf-8")
    if version != "1.0.2":
        changed = text.replace('Include="IsImageUrlDotNet" Version="[1.0.2]"', f'Include="IsImageUrlDotNet" Version="[{version}]"')
        assert changed != text, "the capture's package line was not found"
        fsproj.write_text(changed, encoding="utf-8")
        print(f"capture-{version}: Capture.fsproj changed in one line, PackageReference IsImageUrlDotNet [1.0.2] -> [{version}]; no other file changed")
    else:
        print("capture-1.0.2: the capture program unchanged")
    code, out, err = run(["dotnet", "build", "-c", "Release", "-nologo", "-v:q", "-tl:off"], copy)
    warnings = sorted(set(re.findall(r"warning (\w+\d+)", out + err)))
    print(f"capture-{version}: build exit {code}; warnings {', '.join(warnings) or 'none'}")
    if code != 0:
        print(mask(out + err))
        sys.exit(1)
    for tfm in TFMS:
        code, out, err = run(["dotnet", "run", "-c", "Release", "-f", tfm, "--no-build"], copy)
        (SCRATCH / f"{version}.{tfm}-{OS}.today.json").write_text(out, encoding="utf-8")
        doc = json.loads(out)
        runs[(version, tfm)] = doc
        print(f"capture-{version} {tfm}: exit {code}, {len(doc['cases'])} cases, assembly {doc['assembly']}, runtime {doc['runtime']}")
print()

for tfm in TFMS:
    name, rec = recording(tfm)
    print(f"## {tfm} against {name} (recorded {rec['captured']}, {rec['runtime']}, assembly {rec['assembly']})")
    for version in ["1.0.2", "2.0.0"]:
        doc = runs[(version, tfm)]
        a, r, b, d, diffs = compare(rec["cases"], doc["cases"])
        n = len(rec["cases"])
        print(f"{version} today: same answer {a} of {n}, same requests {r} of {n}, both {b} of {n}"
              + (f" ({d} answers equal once the drive letter is swapped, as the golden test does)" if d else ""))
        for line in diffs:
            print("  " + mask(line))
    a, r, b, d, diffs = compare(runs[("1.0.2", tfm)]["cases"], runs[("2.0.0", tfm)]["cases"])
    print(f"2.0.0 against 1.0.2, both today: same answer {a}, same requests {r}, both {b} of {len(rec['cases'])}")
    for line in diffs:
        print("  " + mask(line))
    print()

print("## what the recordings hold that package-modernize's NuGet capture template would not have recorded")
for f in sorted(GOLDEN.glob("1.0.2.*.json")):
    cases = json.loads(f.read_text(encoding="utf-8"))["cases"]
    heads = sum(len(c["requests"]) for c in cases)
    with_req = sum(1 for c in cases if c["requests"])
    throws = [c for c in cases if isinstance(c["result"], dict) and "$throws" in c["result"]]
    status = sum(1 for c in throws if "status" in c["result"])
    inner = sum(1 for c in throws if "inner" in c["result"])
    tr = sum(1 for c in cases if c["culture"] == "tr-TR")
    print(f"{f.name}: {len(cases)} cases; {heads} request heads in {with_req} cases; {len(throws)} throws, "
          f"{status} with a WebException status, {inner} with an inner exception; {tr} cases under tr-TR")

net48 = json.loads((GOLDEN / f"1.0.2.net48-windows.json").read_text(encoding="utf-8"))["cases"]
net10 = json.loads((GOLDEN / f"1.0.2.net10.0-windows.json").read_text(encoding="utf-8"))["cases"]
full = sum(1 for x, y in zip(net48, net10) if x != y)
template = sum(1 for x, y in zip(net48, net10) if template_view(x) != template_view(y))
req_only = sum(1 for x, y in zip(net48, net10) if template_view(x) == template_view(y) and x != y)
print(f"1.0.2 on net48 against 1.0.2 on net10.0 (Windows recordings): {full} of {len(net48)} cases differ in the full record, "
      f"{template} in the template's view; {req_only} differ only in what the template drops (requests, status, inner exception)")
