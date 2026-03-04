#!/usr/bin/env python3
"""校验 Controllers 与 Bosch.http 的覆盖关系。

规则：`<Name>Controller.cs` 必须存在同名 `<Name>.http`。
若存在缺失，脚本返回非 0 退出码，供 CI 失败。
"""

from __future__ import annotations

from pathlib import Path
import sys

REPO_ROOT = Path(__file__).resolve().parents[1]
CONTROLLERS_DIR = REPO_ROOT / "src/backend/Atlas.WebApi/Controllers"
HTTP_DIR = REPO_ROOT / "src/backend/Atlas.WebApi/Bosch.http"


def collect_controller_names() -> set[str]:
    names: set[str] = set()
    for path in CONTROLLERS_DIR.glob("*Controller.cs"):
        stem = path.stem
        if not stem.endswith("Controller"):
            continue
        names.add(stem[: -len("Controller")])
    return names


def collect_http_names() -> set[str]:
    return {path.stem for path in HTTP_DIR.glob("*.http")}


def main() -> int:
    if not CONTROLLERS_DIR.exists() or not HTTP_DIR.exists():
        print("[verify-http-coverage] 路径不存在，请确认仓库结构。", file=sys.stderr)
        return 2

    controller_names = collect_controller_names()
    http_names = collect_http_names()

    missing = sorted(controller_names - http_names)

    if missing:
        print("[verify-http-coverage] 缺失同名 HTTP 文件：", file=sys.stderr)
        for name in missing:
            print(f"  - {name}.http", file=sys.stderr)
        return 1

    print("[verify-http-coverage] 覆盖校验通过：所有 Controller 均存在同名 HTTP 文件。")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
