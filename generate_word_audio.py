#!/usr/bin/env python3
# If edge-tts is not installed, run:
# pip install edge-tts

from __future__ import annotations

import asyncio
import json
import re
from pathlib import Path
from typing import Any

try:
    import edge_tts
except ImportError:
    edge_tts = None


WORDS_DIR = Path(r"D:\study\Swirl\backend\Swirl.Api\wwwroot\media\audio\words")
WORDS_FILE = WORDS_DIR / "words.json"

VOICE = "en-US-AriaNeural"
DELAY_SECONDS = 0.3
BAD_FILENAME_CHARS_RE = re.compile(r"[^a-z0-9_-]+")
MULTIPLE_UNDERSCORES_RE = re.compile(r"_+")


def make_mp3_filename(word: str) -> str:
    cleaned = word.strip().lower()
    cleaned = BAD_FILENAME_CHARS_RE.sub("_", cleaned)
    cleaned = MULTIPLE_UNDERSCORES_RE.sub("_", cleaned).strip("_")
    return f"{cleaned}.mp3"


def find_english_words(data: Any) -> list[str]:
    if isinstance(data, list):
        words: list[str] = []
        for item in data:
            words.extend(find_english_words(item))
        return words

    if isinstance(data, dict):
        english = data.get("english")
        if isinstance(english, str) and english.strip():
            return [english.strip()]

        words = []
        for value in data.values():
            words.extend(find_english_words(value))
        return words

    return []


def load_words() -> list[str]:
    with WORDS_FILE.open("r", encoding="utf-8") as file:
        data = json.load(file)

    return find_english_words(data)


async def generate_one_word(word: str, output_path: Path) -> None:
    if edge_tts is None:
        raise RuntimeError("edge-tts is not installed. Run: pip install edge-tts")

    communicate = edge_tts.Communicate(text=word, voice=VOICE)
    await communicate.save(str(output_path))


async def main() -> None:
    WORDS_DIR.mkdir(parents=True, exist_ok=True)

    words = load_words()
    created = 0
    skipped = 0
    errors = 0

    for word in words:
        filename = make_mp3_filename(word)
        if filename == ".mp3":
            errors += 1
            print(f"error: {word} -> empty file name")
            continue

        output_path = WORDS_DIR / filename

        if output_path.exists():
            skipped += 1
            print(f"skip: {word}")
            continue

        try:
            await generate_one_word(word, output_path)
        except Exception as error:
            errors += 1
            print(f"error: {word} -> {error}")
        else:
            created += 1
            print(f"done: {word} -> {output_path}")
        finally:
            await asyncio.sleep(DELAY_SECONDS)

    print()
    print("Statistics:")
    print(f"words found: {len(words)}")
    print(f"files created: {created}")
    print(f"skipped: {skipped}")
    print(f"errors: {errors}")


if __name__ == "__main__":
    asyncio.run(main())
