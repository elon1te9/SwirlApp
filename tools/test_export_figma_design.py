from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path


SCRIPT_PATH = Path(__file__).with_name("export_figma_design.py")
SPEC = importlib.util.spec_from_file_location("export_figma_design", SCRIPT_PATH)
export_figma_design = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(export_figma_design)


class FigmaSummaryTests(unittest.TestCase):
    def test_summary_uses_frame_relative_coordinates_for_nested_nodes(self) -> None:
        frame = {
            "name": "Sample",
            "type": "FRAME",
            "backgroundColor": {"r": 1, "g": 1, "b": 1, "a": 1},
            "absoluteBoundingBox": {"x": 1000, "y": 2000, "width": 393, "height": 852},
            "children": [
                {
                    "name": "Card",
                    "type": "FRAME",
                    "fills": [{"type": "SOLID", "color": {"r": 0.5, "g": 0.5, "b": 0.5, "a": 1}}],
                    "cornerRadius": 12,
                    "absoluteBoundingBox": {"x": 1010, "y": 2015, "width": 200, "height": 100},
                    "children": [
                        {
                            "name": "Title",
                            "type": "TEXT",
                            "characters": "Hello",
                            "fills": [{"type": "SOLID", "color": {"r": 0, "g": 0, "b": 0, "a": 1}}],
                            "style": {
                                "fontFamily": "Outfit",
                                "fontWeight": 700,
                                "fontSize": 24,
                                "lineHeightPx": 30,
                                "letterSpacing": 0,
                            },
                            "absoluteBoundingBox": {"x": 1025, "y": 2030, "width": 80, "height": 30},
                        }
                    ],
                }
            ],
        }

        summary = export_figma_design.build_markdown_summary("Sample", "1:2", frame)

        self.assertIn("- Relative position: x=10, y=15", summary)
        self.assertIn("- Debug absolute position: x=1010, y=2015", summary)
        self.assertIn("#### Title", summary)
        self.assertIn("- Text content: Hello", summary)
        self.assertIn("- Relative position: x=25, y=30", summary)
        self.assertIn("- Font family: Outfit", summary)
        self.assertIn("fontFamily=Outfit", summary)
        self.assertIn("fontWeight=700", summary)
        self.assertIn("fontSize=24", summary)
        self.assertIn("lineHeightPx=30", summary)
        self.assertIn("letterSpacing=0", summary)
        self.assertIn("- Fill colors: #000000 (alpha 1)", summary)


if __name__ == "__main__":
    unittest.main()
