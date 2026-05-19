from __future__ import annotations

import json
import os
import re
import sys
from pathlib import Path
from typing import Any

try:
    import requests
except ImportError:  # pragma: no cover - depends on local environment
    requests = None


ROOT_DIR = Path(__file__).resolve().parents[1]
FIGMA_DIR = ROOT_DIR / "frontend" / "design" / "figma"
NODES_FILE = FIGMA_DIR / "nodes.json"
RAW_NODES_DIR = FIGMA_DIR / "nodes"
SCREENSHOTS_DIR = ROOT_DIR / "frontend" / "design" / "screenshots"
SPECS_DIR = ROOT_DIR / "frontend" / "design" / "specs"

FIGMA_API_BASE = "https://api.figma.com/v1"
MAX_SUMMARY_DEPTH = 4


def main() -> int:
    if requests is None:
        print("Missing dependency: requests. Install it with: python -m pip install requests", file=sys.stderr)
        return 1

    token = require_env("FIGMA_TOKEN")
    file_key = require_env("FIGMA_FILE_KEY")
    node_entries = read_node_entries(NODES_FILE)

    ensure_output_dirs()

    node_ids = [entry["id"] for entry in node_entries]
    nodes_response = get_figma_json(
        f"{FIGMA_API_BASE}/files/{file_key}/nodes",
        token,
        params={"ids": ",".join(node_ids)},
    )

    figma_nodes = nodes_response.get("nodes", {})
    exports = []
    for entry in node_entries:
        node_id = entry["id"]
        node_payload = figma_nodes.get(node_id)
        if not node_payload:
            print(f"Warning: node {node_id} was not returned by Figma", file=sys.stderr)
            continue

        document = node_payload.get("document", {})
        screen_name = entry.get("name") or document.get("name") or node_id
        basename = build_basename(screen_name, node_id)
        screenshot_path = SCREENSHOTS_DIR / f"{basename}.png"
        exports.append(
            {
                "node_id": node_id,
                "node_payload": node_payload,
                "document": document,
                "screen_name": screen_name,
                "basename": basename,
                "screenshot_path": screenshot_path,
            }
        )

    missing_screenshot_ids = [item["node_id"] for item in exports if not item["screenshot_path"].exists()]
    image_response = {"images": {}}
    if missing_screenshot_ids:
        image_response = get_figma_json(
            f"{FIGMA_API_BASE}/images/{file_key}",
            token,
            params={"ids": ",".join(missing_screenshot_ids), "format": "png", "scale": "2"},
        )

    figma_images = image_response.get("images", {})

    for item in exports:
        node_id = item["node_id"]
        node_payload = item["node_payload"]
        document = item["document"]
        screen_name = item["screen_name"]
        basename = item["basename"]
        screenshot_path = item["screenshot_path"]

        raw_path = RAW_NODES_DIR / f"{basename}.json"
        write_json(raw_path, node_payload)

        if not screenshot_path.exists():
            image_url = figma_images.get(node_id)
            if image_url:
                download_file(image_url, screenshot_path)
            else:
                print(f"Warning: PNG export URL is missing for node {node_id}", file=sys.stderr)

        spec_path = SPECS_DIR / f"{basename}.md"
        spec_path.write_text(build_markdown_summary(screen_name, node_id, document), encoding="utf-8")

        print(f"Exported {screen_name} ({node_id})")

    return 0


def require_env(name: str) -> str:
    value = os.environ.get(name)
    if not value:
        raise SystemExit(f"Environment variable {name} is required.")
    return value


def read_node_entries(path: Path) -> list[dict[str, str]]:
    if not path.exists():
        raise SystemExit(f"Node file not found: {path}")

    data = json.loads(path.read_text(encoding="utf-8"))

    if isinstance(data, list):
        return normalize_node_list(data)

    if isinstance(data, dict) and isinstance(data.get("nodes"), list):
        return normalize_node_list(data["nodes"])

    raise SystemExit("nodes.json must contain either an array or an object with a 'nodes' array.")


def normalize_node_list(items: list[Any]) -> list[dict[str, str]]:
    entries: list[dict[str, str]] = []

    for item in items:
        if isinstance(item, str):
            node_id = item.strip()
            name = ""
        elif isinstance(item, dict):
            node_id = str(item.get("id", "")).strip()
            name = str(item.get("name", "")).strip()
        else:
            raise SystemExit("Each node entry must be a string or an object with an 'id'.")

        if not node_id:
            raise SystemExit("Each node entry must have a non-empty id.")

        entries.append({"id": node_id, "name": name})

    if not entries:
        raise SystemExit("nodes.json does not contain any node ids.")

    return entries


def ensure_output_dirs() -> None:
    RAW_NODES_DIR.mkdir(parents=True, exist_ok=True)
    SCREENSHOTS_DIR.mkdir(parents=True, exist_ok=True)
    SPECS_DIR.mkdir(parents=True, exist_ok=True)


def get_figma_json(url: str, token: str, params: dict[str, str]) -> dict[str, Any]:
    response = requests.get(url, headers={"X-Figma-Token": token}, params=params, timeout=60)
    response.raise_for_status()
    return response.json()


def download_file(url: str, path: Path) -> None:
    response = requests.get(url, timeout=120)
    response.raise_for_status()
    path.write_bytes(response.content)


def write_json(path: Path, data: Any) -> None:
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2), encoding="utf-8")


def build_markdown_summary(screen_name: str, node_id: str, node: dict[str, Any]) -> str:
    box = node.get("absoluteBoundingBox") or {}
    lines = [
        f"# {screen_name}",
        "",
        f"- Node id: `{node_id}`",
        f"- Frame size: {format_size(box)}",
        f"- Background color: {format_background(node)}",
        f"- Frame absolute position: x={format_number(box.get('x'))}, y={format_number(box.get('y'))}",
        "",
        "## Nodes",
        "",
    ]

    children = node.get("children") or []
    if not children:
        lines.append("No child nodes found.")
        lines.append("")
        return "\n".join(lines)

    for child in children:
        lines.extend(format_node(child, box, depth=1))
        lines.append("")

    return "\n".join(lines)


def format_node(node: dict[str, Any], root_box: dict[str, Any], depth: int) -> list[str]:
    if depth > MAX_SUMMARY_DEPTH or should_skip_node(node):
        return []

    name = node.get("name", "Unnamed")
    node_type = node.get("type", "UNKNOWN")
    box = node.get("absoluteBoundingBox") or {}
    heading_level = min(depth + 2, 5)
    heading = "#" * heading_level

    lines = [
        f"{heading} {name}",
        "",
        f"- Type: {node_type}",
        f"- Relative position: x={format_relative_number(box.get('x'), root_box.get('x'))}, y={format_relative_number(box.get('y'), root_box.get('y'))}",
        f"- Size: width={format_number(box.get('width'))}, height={format_number(box.get('height'))}",
        f"- Debug absolute position: x={format_number(box.get('x'))}, y={format_number(box.get('y'))}",
    ]

    if node_type == "TEXT":
        text = node.get("characters")
        if text is not None:
            lines.append(f"- Text content: {format_inline_text(text)}")
        lines.extend(format_text_style_lines(node.get("style")))

    text_style = format_text_style(node.get("style"))
    if text_style:
        lines.append(f"- Text style: {text_style}")

    fills = format_fills(node.get("fills"))
    if fills:
        lines.append(f"- Fill colors: {fills}")

    radius = format_corner_radius(node)
    if radius:
        lines.append(f"- Corner radius: {radius}")

    children = node.get("children") or []
    for child in children:
        child_lines = format_node(child, root_box, depth + 1)
        if child_lines:
            lines.append("")
            lines.extend(child_lines)

    return lines


def should_skip_node(node: dict[str, Any]) -> bool:
    name = str(node.get("name", "")).strip()
    text = str(node.get("characters", "")).strip()
    box = node.get("absoluteBoundingBox") or {}
    width = safe_float(box.get("width"))
    height = safe_float(box.get("height"))
    is_tiny = width is not None and height is not None and width < 1 and height < 1
    return is_tiny and not name and not text


def format_size(box: dict[str, Any]) -> str:
    width = format_number(box.get("width"))
    height = format_number(box.get("height"))
    return f"{width} x {height}"


def format_background(node: dict[str, Any]) -> str:
    color = node.get("backgroundColor")
    if color:
        return format_color(color)

    fills = format_fills(node.get("fills"))
    return fills or "not available"


def format_text_style(style: dict[str, Any] | None) -> str:
    if not style:
        return ""

    fields = [
        "fontFamily",
        "fontPostScriptName",
        "fontWeight",
        "fontSize",
        "lineHeightPx",
        "letterSpacing",
        "textAlignHorizontal",
        "textAlignVertical",
    ]
    values = []
    for field in fields:
        value = style.get(field)
        if value is not None:
            values.append(f"{field}={value}")

    return ", ".join(values)


def format_text_style_lines(style: dict[str, Any] | None) -> list[str]:
    if not style:
        return []

    labels = [
        ("fontFamily", "Font family"),
        ("fontWeight", "Font weight"),
        ("fontSize", "Font size"),
        ("lineHeightPx", "Line height px"),
        ("letterSpacing", "Letter spacing"),
    ]
    lines = []
    for key, label in labels:
        value = style.get(key)
        if value is not None:
            lines.append(f"- {label}: {format_style_value(value)}")
    return lines


def format_style_value(value: Any) -> str:
    number = safe_float(value)
    if number is None:
        return str(value)
    return format_number(number)


def format_fills(fills: list[dict[str, Any]] | None) -> str:
    if not fills:
        return ""

    colors = []
    for fill in fills:
        if not fill.get("visible", True):
            continue
        color = fill.get("color")
        if color:
            colors.append(format_color(color))

    return ", ".join(colors)


def format_corner_radius(node: dict[str, Any]) -> str:
    if node.get("cornerRadius") is not None:
        return str(node["cornerRadius"])

    radii = node.get("rectangleCornerRadii")
    if radii:
        return ", ".join(str(value) for value in radii)

    return ""


def format_color(color: dict[str, Any]) -> str:
    red = to_channel(color.get("r"))
    green = to_channel(color.get("g"))
    blue = to_channel(color.get("b"))
    alpha = color.get("a", 1)
    alpha_text = format_number(alpha)
    return f"#{red:02X}{green:02X}{blue:02X} (alpha {alpha_text})"


def to_channel(value: Any) -> int:
    if value is None:
        return 0
    return max(0, min(255, round(float(value) * 255)))


def format_number(value: Any) -> str:
    number = safe_float(value)
    if number is None:
        return "not available"
    if number.is_integer():
        return str(int(number))
    return f"{number:.2f}"


def format_relative_number(value: Any, root_value: Any) -> str:
    number = safe_float(value)
    root_number = safe_float(root_value)
    if number is None or root_number is None:
        return "not available"
    return format_number(number - root_number)


def safe_float(value: Any) -> float | None:
    if value is None:
        return None
    try:
        return float(value)
    except (TypeError, ValueError):
        return None


def format_inline_text(value: Any) -> str:
    text = str(value).replace("\r\n", "\n").replace("\r", "\n")
    text = text.replace("\n", "\\n")
    return text


def build_basename(screen_name: str, node_id: str) -> str:
    name = slugify(screen_name) or "screen"
    safe_node_id = node_id.replace(":", "-")
    return f"{name}__{safe_node_id}"


def slugify(value: str) -> str:
    value = value.lower().strip()
    value = re.sub(r"[^a-z0-9]+", "-", value, flags=re.IGNORECASE)
    value = re.sub(r"-+", "-", value)
    return value.strip("-")


if __name__ == "__main__":
    raise SystemExit(main())
