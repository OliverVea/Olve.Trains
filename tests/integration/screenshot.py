from __future__ import annotations

import shutil
from dataclasses import dataclass
from pathlib import Path

import numpy as np
from PIL import Image


@dataclass
class DiffResult:
    similarity: float
    mean_diff: float
    max_diff: int
    changed_pixels_pct: float
    passed: bool
    diff_image_path: Path | None = None

    def summary(self) -> str:
        status = "PASS" if self.passed else "FAIL"
        return (
            f"[{status}] similarity={self.similarity:.4f} "
            f"mean_diff={self.mean_diff:.2f} max_diff={self.max_diff} "
            f"changed={self.changed_pixels_pct:.2%}"
        )


def compare_screenshots(
    actual: Path,
    reference: Path,
    threshold: float = 0.995,
    pixel_tolerance: int = 2,
    diff_output: Path | None = None,
) -> DiffResult:
    actual_img = Image.open(actual).convert("RGB")
    ref_img = Image.open(reference).convert("RGB")

    if actual_img.size != ref_img.size:
        raise ValueError(
            f"Size mismatch: actual={actual_img.size} reference={ref_img.size}"
        )

    actual_arr = np.array(actual_img, dtype=np.int16)
    ref_arr = np.array(ref_img, dtype=np.int16)

    diff = np.abs(actual_arr - ref_arr)

    mean_diff = float(np.mean(diff))
    max_diff = int(np.max(diff))

    pixel_diffs = np.max(diff, axis=2)
    changed_mask = pixel_diffs > pixel_tolerance
    changed_pixels_pct = float(np.mean(changed_mask))

    similarity = 1.0 - changed_pixels_pct
    passed = similarity >= threshold

    diff_image_path = None
    if diff_output or not passed:
        diff_image_path = diff_output or actual.with_name(
            actual.stem + "-diff" + actual.suffix
        )
        _save_diff_image(actual_arr, ref_arr, changed_mask, diff_image_path)

    return DiffResult(
        similarity=similarity,
        mean_diff=mean_diff,
        max_diff=max_diff,
        changed_pixels_pct=changed_pixels_pct,
        passed=passed,
        diff_image_path=diff_image_path,
    )


def assert_screenshot_matches(
    actual: Path,
    reference: Path,
    threshold: float = 0.995,
    pixel_tolerance: int = 2,
) -> None:
    if not reference.exists():
        raise FileNotFoundError(
            f"Reference screenshot not found: {reference}\n"
            f"Run with --update-references to generate it."
        )

    result = compare_screenshots(
        actual, reference, threshold=threshold, pixel_tolerance=pixel_tolerance
    )

    if not result.passed:
        msg = (
            f"Screenshot mismatch: {result.summary()}\n"
            f"  actual:    {actual}\n"
            f"  reference: {reference}\n"
        )
        if result.diff_image_path:
            msg += f"  diff:      {result.diff_image_path}\n"
        raise AssertionError(msg)


def update_reference(actual: Path, reference: Path) -> None:
    reference.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(actual, reference)
    print(f"  Updated reference: {reference}")


def _save_diff_image(
    actual_arr: np.ndarray,
    ref_arr: np.ndarray,
    changed_mask: np.ndarray,
    output_path: Path,
) -> None:
    h, w = changed_mask.shape

    diff_img = np.zeros((h, w, 3), dtype=np.uint8)

    dimmed = (ref_arr.astype(np.uint8) // 3).astype(np.uint8)
    diff_img[~changed_mask] = dimmed[~changed_mask]

    abs_diff = np.abs(actual_arr - ref_arr).astype(np.float64)
    intensity = np.clip(np.max(abs_diff, axis=2) * 4, 0, 255).astype(np.uint8)
    diff_img[changed_mask, 0] = intensity[changed_mask]
    diff_img[changed_mask, 1] = 0
    diff_img[changed_mask, 2] = 0

    Image.fromarray(diff_img).save(output_path)
