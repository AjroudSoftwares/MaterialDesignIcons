#!/usr/bin/env python3
"""
Generate Material Design Icons font mappings for .NET MAUI.

Dependency hint:
    pip install requests
"""

from __future__ import annotations

import argparse
import datetime as dt
import html
import json
import re
import sys
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, List, Tuple

try:
    import requests
except ImportError as exc:  # pragma: no cover
    raise SystemExit(
        "Missing dependency: requests\n"
        "Install it with: pip install requests"
    ) from exc


LATEST_RELEASE_API = "https://api.github.com/repos/google/material-design-icons/releases/latest"
RAW_BASE = "https://raw.githubusercontent.com/google/material-design-icons/{ref}/{path}"


@dataclass(frozen=True)
class VariantConfig:
    key: str
    class_name: str
    generated_file: str
    font_file: str
    codepoints_file: str
    font_repo_paths: Tuple[str, ...]
    codepoints_repo_path: str


VARIANTS: Tuple[VariantConfig, ...] = (
    VariantConfig(
        key="Regular",
        class_name="Regular",
        generated_file="MaterialIcons.Regular.cs",
        font_file="MaterialIcons-Regular.ttf",
        codepoints_file="MaterialIcons-Regular.codepoints",
        font_repo_paths=("font/MaterialIcons-Regular.ttf",),
        codepoints_repo_path="font/MaterialIcons-Regular.codepoints",
    ),
    VariantConfig(
        key="Outlined",
        class_name="Outlined",
        generated_file="MaterialIcons.Outlined.cs",
        font_file="MaterialIconsOutlined-Regular.ttf",
        codepoints_file="MaterialIconsOutlined-Regular.codepoints",
        font_repo_paths=(
            "font/MaterialIconsOutlined-Regular.ttf",
            "font/MaterialIconsOutlined-Regular.otf",
        ),
        codepoints_repo_path="font/MaterialIconsOutlined-Regular.codepoints",
    ),
    VariantConfig(
        key="Rounded",
        class_name="Rounded",
        generated_file="MaterialIcons.Rounded.cs",
        font_file="MaterialIconsRound-Regular.ttf",
        codepoints_file="MaterialIconsRound-Regular.codepoints",
        font_repo_paths=(
            "font/MaterialIconsRound-Regular.ttf",
            "font/MaterialIconsRound-Regular.otf",
        ),
        codepoints_repo_path="font/MaterialIconsRound-Regular.codepoints",
    ),
    VariantConfig(
        key="Sharp",
        class_name="Sharp",
        generated_file="MaterialIcons.Sharp.cs",
        font_file="MaterialIconsSharp-Regular.ttf",
        codepoints_file="MaterialIconsSharp-Regular.codepoints",
        font_repo_paths=(
            "font/MaterialIconsSharp-Regular.ttf",
            "font/MaterialIconsSharp-Regular.otf",
        ),
        codepoints_repo_path="font/MaterialIconsSharp-Regular.codepoints",
    ),
)


IDENTIFIER_PATTERN = re.compile(r"[^a-zA-Z0-9_]")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Generate Material Design Icons C# mappings for this MAUI library."
    )
    parser.add_argument(
        "--skip-download",
        action="store_true",
        help="Skip downloading fonts/codepoints and use local files in tools/codepoints.",
    )
    parser.add_argument(
        "--output-dir",
        default="Resources/Fonts",
        help="Output directory for generated C# files and downloaded fonts (default: Resources/Fonts).",
    )
    return parser.parse_args()


def ensure_dir(path: Path) -> None:
    path.mkdir(parents=True, exist_ok=True)


def get_latest_ref() -> str:
    try:
        response = requests.get(
            LATEST_RELEASE_API,
            timeout=20,
            headers={"Accept": "application/vnd.github+json"},
        )
        if response.status_code == 200:
            payload = response.json()
            tag = str(payload.get("tag_name") or "").strip()
            if tag:
                return tag
        elif response.status_code not in (404, 410):
            response.raise_for_status()
    except Exception:
        pass
    return "master"


def can_download_from_ref(ref: str, repo_path: str) -> bool:
    url = RAW_BASE.format(ref=ref, path=repo_path)
    try:
        response = requests.head(url, timeout=20)
        if response.status_code == 200:
            return True
        if response.status_code in (403, 405):
            # Some endpoints may reject HEAD; fall back to a lightweight GET.
            fallback = requests.get(url, timeout=20, stream=True)
            ok = fallback.status_code == 200
            fallback.close()
            return ok
        return False
    except Exception:
        return False


def resolve_download_ref(preferred_ref: str) -> str:
    probe_path = VARIANTS[0].codepoints_repo_path
    candidates = [preferred_ref, "master", "main"]
    seen = set()
    for ref in candidates:
        if ref in seen:
            continue
        seen.add(ref)
        if can_download_from_ref(ref, probe_path):
            return ref
    raise RuntimeError(
        "Unable to locate a valid upstream ref for material-design-icons "
        "(tried latest release tag, master, main)."
    )


def download_file(ref: str, repo_path: str, destination: Path) -> None:
    url = RAW_BASE.format(ref=ref, path=repo_path)
    response = requests.get(url, timeout=60)
    response.raise_for_status()
    destination.write_bytes(response.content)


def download_first_available(ref: str, repo_paths: Tuple[str, ...], destination: Path) -> None:
    last_error: Exception | None = None
    for repo_path in repo_paths:
        try:
            download_file(ref, repo_path, destination)
            return
        except requests.HTTPError as ex:
            response = getattr(ex, "response", None)
            if response is not None and response.status_code == 404:
                last_error = ex
                continue
            raise

    if last_error is not None:
        raise last_error
    raise RuntimeError(f"No candidate source paths configured for {destination.name}")


def to_pascal_case(snake_name: str) -> str:
    parts = [p for p in snake_name.strip().split("_") if p]
    if not parts:
        return "_"
    pascal = "".join(part[:1].upper() + part[1:] for part in parts)
    pascal = IDENTIFIER_PATTERN.sub("", pascal)
    if not pascal:
        pascal = "_"
    if pascal[0].isdigit():
        pascal = f"_{pascal}"
    return pascal


def to_display_name(snake_name: str) -> str:
    tokens = [token for token in snake_name.split("_") if token]
    if not tokens:
        return snake_name
    return " ".join(token[:1].upper() + token[1:] for token in tokens)


def parse_codepoints_file(path: Path) -> List[Dict[str, str]]:
    icons: List[Dict[str, str]] = []
    seen_identifiers: Dict[str, int] = {}

    with path.open("r", encoding="utf-8") as handle:
        for raw_line in handle:
            line = raw_line.strip()
            if not line:
                continue
            parts = line.split()
            if len(parts) < 2:
                continue

            snake_name, hex_code = parts[0], parts[1].lower()
            pascal = to_pascal_case(snake_name)

            count = seen_identifiers.get(pascal, 0)
            seen_identifiers[pascal] = count + 1
            if count > 0:
                pascal = f"{pascal}_{count + 1}"

            icons.append(
                {
                    "snake": snake_name,
                    "pascal": pascal,
                    "display": to_display_name(snake_name),
                    "code": hex_code,
                }
            )

    return icons


def header_lines(version: str) -> List[str]:
    generated_at = dt.datetime.now(dt.timezone.utc).astimezone().strftime("%Y-%m-%d %H:%M:%S %z")
    return [
        "// <auto-generated />",
        f"// Generated: {generated_at} | Material Design Icons {version}",
        "// Do not edit manually — run tools/generate_icons.py to regenerate.",
        "#nullable enable",
        "",
    ]


def generate_metadata_text(
    all_variant_icons: Dict[str, List[Dict[str, str]]],
    version: str,
) -> str:
    generated_at = dt.datetime.now(dt.timezone.utc).astimezone().strftime("%Y-%m-%d %H:%M:%S %z")
    lines = [
        f"# Generated: {generated_at} | Material Design Icons {version}",
        "# Format: Variant<TAB>Snake<TAB>Pascal<TAB>HexCode",
    ]

    entries: List[Tuple[str, str, str, str]] = []
    for variant_name, icons in all_variant_icons.items():
        for icon in icons:
            entries.append((variant_name, icon["snake"], icon["pascal"], icon["code"]))

    entries.sort(key=lambda item: (item[0], item[1], item[2]))

    for variant_name, snake_name, pascal_name, hex_code in entries:
        lines.append(f"{variant_name}\t{snake_name}\t{pascal_name}\t{hex_code}")

    lines.append("")
    return "\n".join(lines)


def find_csproj(project_root: Path) -> Path:
    candidates = sorted(project_root.glob("*.csproj"))
    if not candidates:
        raise FileNotFoundError(f"No .csproj file found in {project_root}")
    return candidates[0]


def _local_name(tag: str) -> str:
    if "}" in tag:
        return tag.split("}", 1)[1]
    return tag


def update_csproj_fonts(csproj_path: Path) -> None:
    required_includes = [
        r"Resources\Fonts\MaterialIcons-Regular.ttf",
        r"Resources\Fonts\MaterialIconsOutlined-Regular.ttf",
        r"Resources\Fonts\MaterialIconsRound-Regular.ttf",
        r"Resources\Fonts\MaterialIconsSharp-Regular.ttf",
    ]

    tree = ET.parse(csproj_path)
    root = tree.getroot()

    existing = {
        item.attrib.get("Include", "")
        for item in root.iter()
        if _local_name(item.tag) == "MauiFont"
    }

    missing = [include for include in required_includes if include not in existing]
    if not missing:
        return

    item_groups = [
        node
        for node in root
        if _local_name(node.tag) == "ItemGroup"
    ]

    target_group = None
    for group in item_groups:
        if any(_local_name(child.tag) == "MauiFont" for child in list(group)):
            target_group = group
            break

    if target_group is None:
        target_group = ET.SubElement(root, "ItemGroup")

    for include in missing:
        ET.SubElement(target_group, "MauiFont", {"Include": include})

    try:
        ET.indent(tree, space="\t", level=0)  # Python 3.9+
    except Exception:
        pass

    tree.write(csproj_path, encoding="utf-8", xml_declaration=False)


def validate_required_files(codepoints_dir: Path) -> None:
    missing = [
        variant.codepoints_file
        for variant in VARIANTS
        if not (codepoints_dir / variant.codepoints_file).exists()
    ]
    if missing:
        missing_list = "\n".join(f"- {name}" for name in missing)
        raise FileNotFoundError(
            "Missing codepoints files in tools/codepoints while using --skip-download:\n"
            f"{missing_list}"
        )


def run() -> int:
    print("Dependency hint: pip install requests")
    args = parse_args()

    script_path = Path(__file__).resolve()
    tools_dir = script_path.parent
    project_root = tools_dir.parent

    output_dir = Path(args.output_dir)
    if not output_dir.is_absolute():
        output_dir = project_root / output_dir
    output_dir = output_dir.resolve()

    codepoints_dir = tools_dir / "codepoints"
    ensure_dir(output_dir)
    ensure_dir(codepoints_dir)

    latest_ref = get_latest_ref()
    download_ref = resolve_download_ref(latest_ref)

    if not args.skip_download:
        for variant in VARIANTS:
            download_first_available(
                download_ref,
                variant.font_repo_paths,
                output_dir / variant.font_file,
            )
            download_file(
                download_ref,
                variant.codepoints_repo_path,
                codepoints_dir / variant.codepoints_file,
            )
    else:
        validate_required_files(codepoints_dir)

    variant_icons: Dict[str, List[Dict[str, str]]] = {}

    for variant in VARIANTS:
        codepoints_path = codepoints_dir / variant.codepoints_file
        icons = parse_codepoints_file(codepoints_path)
        variant_icons[variant.class_name] = icons

    metadata_path = output_dir / "MaterialIcons.metadata.txt"
    metadata_content = generate_metadata_text(variant_icons, download_ref)
    metadata_path.write_text(metadata_content, encoding="utf-8")

    obsolete_generated_files = [
        output_dir / "MaterialIcons.Regular.cs",
        output_dir / "MaterialIcons.Outlined.cs",
        output_dir / "MaterialIcons.Rounded.cs",
        output_dir / "MaterialIcons.Sharp.cs",
        output_dir / "MaterialIconsMap.cs",
    ]
    for obsolete_path in obsolete_generated_files:
        if obsolete_path.exists():
            obsolete_path.unlink()

    csproj_path = find_csproj(project_root)
    update_csproj_fonts(csproj_path)

    try:
        rel_output_dir = output_dir.relative_to(project_root).as_posix()
    except ValueError:
        rel_output_dir = output_dir.as_posix()

    total_icons = 0
    for variant in VARIANTS:
        count = len(variant_icons[variant.class_name])
        total_icons += count
        print(f"\u2705 {variant.class_name:<9} — {count:,} icons")

    print(
        f"\U0001f4c4 Metadata  — {total_icons:,} rows → "
        f"{rel_output_dir}/MaterialIcons.metadata.txt"
    )
    print(f"\U0001f4e6 Fonts     → {rel_output_dir}/")
    print(f"\U0001f3f7\ufe0f  Version  : {latest_ref}")

    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(run())
    except requests.HTTPError as http_error:
        response = getattr(http_error, "response", None)
        if response is not None:
            try:
                details = response.json()
                body = json.dumps(details, indent=2)
            except Exception:
                body = response.text
            print(f"HTTP error {response.status_code}:\n{body}", file=sys.stderr)
        else:
            print(str(http_error), file=sys.stderr)
        raise SystemExit(1)
    except Exception as ex:
        print(f"Error: {ex}", file=sys.stderr)
        raise SystemExit(1)
