from pathlib import Path
import ast
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SKILL = ROOT / "skills/unity-editor-design"
errors = []

entry = SKILL / "SKILL.md"
text = entry.read_text(encoding="utf-8")
if not text.startswith("---\n") or "name: unity-editor-design\n" not in text:
    errors.append("Invalid skill frontmatter or name")
if "description:" not in text or "license: MIT" not in text:
    errors.append("Missing skill description or license")
if len(text.splitlines()) >= 500:
    errors.append("SKILL.md should remain below 500 lines")
if (SKILL / "LICENSE").read_bytes() != (ROOT / "LICENSE").read_bytes():
    errors.append("Bundled license differs from root license")

for path in ROOT.rglob("*"):
    if ".git" in path.parts or not path.is_file():
        continue
    if path.suffix.lower() in {".ttf", ".ttc", ".otf", ".dll", ".pdb", ".exe"}:
        errors.append(f"Unexpected bundled binary/font: {path.relative_to(ROOT)}")
    if path.suffix.lower() not in {".md", ".py", ".ps1", ".cs", ".txt"}:
        continue
    content = path.read_text(encoding="utf-8")
    if re.search(r"[A-Za-z]:[/\\](?:workspace|Users)[/\\]", content, re.I):
        errors.append(f"Local absolute path: {path.relative_to(ROOT)}")
    if path.suffix == ".md":
        for target in re.findall(r"\]\(([^)]+)\)", content):
            if "://" in target or target.startswith(("#", "mailto:")):
                continue
            target = target.split("#", 1)[0]
            if target and not (path.parent / target).exists():
                errors.append(f"Broken link in {path.relative_to(ROOT)}: {target}")
    if path.suffix == ".py":
        ast.parse(content, filename=str(path))

for error in errors:
    print("FAIL", error)
if errors:
    sys.exit(1)
print("PASS: skill metadata, licenses, local links, Python syntax and publication file checks")
