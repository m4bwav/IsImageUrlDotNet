"""Checks that every csharp and fsharp block on the wiki pages is, byte for byte, one of the snippets in the
verification program (whose raw strings indent each line by four spaces)."""
import pathlib
import re
import sys

wiki = pathlib.Path(sys.argv[1])
program = pathlib.Path(sys.argv[2]).read_text(encoding="utf-8")
unindented = "\n".join(line[4:] if line.startswith("    ") else line for line in program.split("\n"))
missing = 0
total = 0
for page in sorted(wiki.glob("*.md")):
    for lang, body in re.findall(r"```(csharp|fsharp)\n(.*?)```", page.read_text(encoding="utf-8"), re.S):
        total += 1
        if body.rstrip("\n") not in unindented:
            missing += 1
            print(f"{page.name}: {lang} block not in the program:\n{body}")
print(f"snippets: {total} code blocks, {missing} not in the program")
sys.exit(1 if missing else 0)
