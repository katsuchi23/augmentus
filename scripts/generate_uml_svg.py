#!/usr/bin/env python3

from __future__ import annotations

from pathlib import Path
from xml.sax.saxutils import escape


OUTPUT_PATH = Path("/home/rey/augmentus/docs/trajectory-planning-uml.svg")


BOXES = [
    {
        "id": "settings",
        "title": "MotionProfileSettings",
        "subtitle": "struct",
        "x": 30,
        "y": 70,
        "w": 250,
        "items": [
            "+ sampleInterval : float",
            "+ maxVelocity : float",
            "+ acceleration : float",
            "+ deceleration : float",
            "+ Validate() : void",
        ],
        "style": "box",
    },
    {
        "id": "sample",
        "title": "TrajectorySample",
        "subtitle": "readonly struct",
        "x": 320,
        "y": 70,
        "w": 250,
        "items": [
            "+ Time : float",
            "+ Distance : float",
            "+ Velocity : float",
            "+ Position : Vector3",
        ],
        "style": "box",
    },
    {
        "id": "plan",
        "title": "TrajectoryPlan",
        "subtitle": "class",
        "x": 610,
        "y": 70,
        "w": 320,
        "items": [
            "+ Start : Vector3",
            "+ End : Vector3",
            "+ TotalDistance : float",
            "+ TotalTime : float",
            "+ PeakVelocity : float",
            "+ Samples : IReadOnlyList<TrajectorySample>",
            "+ EvaluatePosition(time) : Vector3",
            "+ EvaluateVelocity(time) : float",
        ],
        "style": "accent",
    },
    {
        "id": "planner",
        "title": "TrapezoidalTrajectoryPlanner",
        "subtitle": "static class",
        "x": 30,
        "y": 300,
        "w": 330,
        "items": [
            "+ Generate(start, end, settings) : TrajectoryPlan",
            "+ Generate(start, end, settings, initialVelocity) : TrajectoryPlan",
        ],
        "style": "accent",
    },
    {
        "id": "demo",
        "title": "TrajectoryDemoController",
        "subtitle": "class : MonoBehaviour",
        "x": 395,
        "y": 280,
        "w": 360,
        "items": [
            "+ RebuildPlan() : void",
            "+ RebuildPlan(preserveCurrentMotion) : void",
            "+ Play() : void",
            "+ HandleControlPointChanged(role) : void",
        ],
        "style": "accent",
    },
    {
        "id": "control",
        "title": "TrajectoryControlPoint",
        "subtitle": "class : MonoBehaviour",
        "x": 790,
        "y": 300,
        "w": 240,
        "items": [
            "+ Initialize(controller, role) : void",
            "+ ControlPointRole",
            "+ DragPlane",
        ],
        "style": "box",
    },
    {
        "id": "camera_switcher",
        "title": "TrajectoryCameraSwitcher",
        "subtitle": "class : MonoBehaviour",
        "x": 30,
        "y": 470,
        "w": 330,
        "items": [
            "+ SetCameras(configuredCameras) : void",
            "+ ActivateCamera(index) : void",
        ],
        "style": "box",
    },
    {
        "id": "camera_controller",
        "title": "TrajectoryCameraController",
        "subtitle": "class : MonoBehaviour",
        "x": 395,
        "y": 500,
        "w": 300,
        "items": [
            "+ Update() : void",
            "+ UpdateRotation() : void",
            "+ UpdateTranslation() : void",
        ],
        "style": "box",
    },
    {
        "id": "bootstrap",
        "title": "TrajectoryDemoBootstrap",
        "subtitle": "static class",
        "x": 730,
        "y": 500,
        "w": 300,
        "items": [
            "+ CreateDemoControllerIfMissing() : void",
        ],
        "style": "box",
    },
]


RELATIONSHIPS = [
    {"x1": 280, "y1": 140, "x2": 320, "y2": 140, "label": "configures", "dashed": False},
    {"x1": 570, "y1": 160, "x2": 610, "y2": 160, "label": "contains", "dashed": False},
    {"x1": 360, "y1": 360, "x2": 395, "y2": 360, "label": "used by", "dashed": True},
    {"x1": 250, "y1": 300, "x2": 250, "y2": 215, "label": "uses", "dashed": False},
    {"x1": 360, "y1": 360, "x2": 610, "y2": 215, "label": "returns", "dashed": False},
    {"x1": 755, "y1": 360, "x2": 790, "y2": 360, "label": "receives drag events from", "dashed": True},
    {"x1": 195, "y1": 470, "x2": 545, "y2": 435, "label": "created/managed by", "dashed": True},
    {"x1": 545, "y1": 500, "x2": 545, "y2": 435, "label": "attached to cameras", "dashed": True},
    {"x1": 880, "y1": 500, "x2": 730, "y2": 415, "label": "creates demo host", "dashed": False},
]


def box_height(items: list[str]) -> int:
    return 74 + (len(items) * 22)


def render_box(box: dict) -> str:
    height = box_height(box["items"])
    x = box["x"]
    y = box["y"]
    w = box["w"]
    title_y = y + 28
    subtitle_y = y + 48
    divider_y = y + 60
    items_y = y + 85

    item_lines = []
    for index, item in enumerate(box["items"]):
        item_y = items_y + (index * 22)
        item_lines.append(f'  <text x="{x + 18}" y="{item_y}" class="text">{escape(item)}</text>')

    return "\n".join(
        [
            f'  <rect x="{x}" y="{y}" width="{w}" height="{height}" class="{box["style"]}" />',
            f'  <text x="{x + 18}" y="{title_y}" class="box-title">{escape(box["title"])}</text>',
            f'  <text x="{x + 18}" y="{subtitle_y}" class="meta">{escape(box["subtitle"])}</text>',
            f'  <line x1="{x + 12}" y1="{divider_y}" x2="{x + w - 12}" y2="{divider_y}" stroke="#284b63" stroke-width="1.5" />',
            *item_lines,
        ]
    )


def render_relationship(rel: dict) -> str:
    css_class = "line dashed" if rel["dashed"] else "line"
    label_x = (rel["x1"] + rel["x2"]) / 2
    label_y = (rel["y1"] + rel["y2"]) / 2 - 8
    return "\n".join(
        [
            f'  <line x1="{rel["x1"]}" y1="{rel["y1"]}" x2="{rel["x2"]}" y2="{rel["y2"]}" class="{css_class}" />',
            f'  <text x="{label_x}" y="{label_y}" text-anchor="middle" class="label">{escape(rel["label"])}</text>',
        ]
    )


def main() -> None:
    svg = f"""<svg xmlns="http://www.w3.org/2000/svg" width="1080" height="700" viewBox="0 0 1080 700">
  <style>
    .title {{ font: 700 20px Arial, sans-serif; fill: #102a43; }}
    .subtitle {{ font: 13px Arial, sans-serif; fill: #486581; }}
    .box-title {{ font: 700 16px Arial, sans-serif; fill: #102a43; }}
    .meta {{ font: italic 12px Arial, sans-serif; fill: #5c677d; }}
    .text {{ font: 13px Arial, sans-serif; fill: #1f2933; }}
    .label {{ font: 12px Arial, sans-serif; fill: #334e68; }}
    .box {{ fill: #f8f5ef; stroke: #284b63; stroke-width: 2; rx: 12; ry: 12; }}
    .accent {{ fill: #e6f4f1; stroke: #197278; stroke-width: 2; rx: 12; ry: 12; }}
    .line {{ stroke: #284b63; stroke-width: 2; fill: none; marker-end: url(#arrow); }}
    .dashed {{ stroke-dasharray: 8 6; }}
  </style>
  <defs>
    <marker id="arrow" markerWidth="10" markerHeight="10" refX="8" refY="3" orient="auto" markerUnits="strokeWidth">
      <path d="M0,0 L0,6 L9,3 z" fill="#284b63" />
    </marker>
  </defs>

  <rect x="0" y="0" width="1080" height="700" fill="#fbfcfe" />
  <text x="30" y="36" class="title">Trajectory Planning UML</text>
  <text x="30" y="58" class="subtitle">Updated for the current Unity runtime demo, discrete replanning, drag controls, and camera utilities.</text>
{chr(10).join(render_box(box) for box in BOXES)}
{chr(10).join(render_relationship(rel) for rel in RELATIONSHIPS)}
</svg>
"""
    OUTPUT_PATH.write_text(svg, encoding="utf-8")


if __name__ == "__main__":
    main()
