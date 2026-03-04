#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
import sys
import uuid
from dataclasses import dataclass
from pathlib import Path
from typing import Any
from urllib import error, request

METHODS = {"GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"}
VAR_PATTERN = re.compile(r"\{\{\s*([^{}\s]+)\s*\}\}")
NAME_PATTERN = re.compile(r"#\s*@name\s+([A-Za-z0-9_\-]+)")
VAR_ASSIGN_PATTERN = re.compile(r"@([A-Za-z0-9_\-]+)\s*=\s*(.*)$")


@dataclass
class RequestBlock:
    file: str
    title: str
    name: str | None
    method: str
    url: str
    headers: dict[str, str]
    body: str


def parse_http_file(path: Path, vars_ctx: dict[str, str]) -> list[RequestBlock]:
    blocks: list[list[str]] = []
    current: list[str] = []
    for raw_line in path.read_text(encoding="utf-8").splitlines():
        if raw_line.strip().startswith("###"):
            if current:
                blocks.append(current)
                current = []
            continue
        current.append(raw_line)
    if current:
        blocks.append(current)

    requests: list[RequestBlock] = []

    for block in blocks:
        method_idx = -1
        req_name: str | None = None
        title = ""

        for idx, line in enumerate(block):
            stripped = line.strip()
            if not stripped:
                continue
            match_name = NAME_PATTERN.search(stripped)
            if match_name:
                req_name = match_name.group(1)
                continue
            if stripped.startswith("@"):
                var_match = VAR_ASSIGN_PATTERN.match(stripped)
                if var_match:
                    key, value = var_match.groups()
                    vars_ctx[key] = value
                continue
            if stripped.startswith("#") and not title:
                title = stripped.lstrip("#").strip()
                continue
            first_token = stripped.split(" ", 1)[0].upper()
            if first_token in METHODS:
                method_idx = idx
                break

        if method_idx < 0:
            continue

        method_line = block[method_idx].strip()
        method, url = method_line.split(" ", 1)

        headers: dict[str, str] = {}
        body_lines: list[str] = []
        in_body = False
        for line in block[method_idx + 1 :]:
            if not in_body and not line.strip():
                in_body = True
                continue
            if not in_body:
                if ":" not in line:
                    continue
                hk, hv = line.split(":", 1)
                headers[hk.strip()] = hv.strip()
            else:
                body_lines.append(line)

        requests.append(
            RequestBlock(
                file=str(path),
                title=title,
                name=req_name,
                method=method.upper(),
                url=url.strip(),
                headers=headers,
                body="\n".join(body_lines).strip(),
            )
        )

    return requests


def extract_json_path(payload: Any, path_expr: str) -> Any:
    if path_expr.startswith("$."):
        path_expr = path_expr[2:]
    elif path_expr == "$":
        return payload

    cursor: Any = payload
    for part in path_expr.split("."):
        if not part:
            continue
        m = re.match(r"([A-Za-z0-9_\-]+)(\[(\d+)\])?$", part)
        if not m:
            return None
        key = m.group(1)
        idx = m.group(3)
        if isinstance(cursor, dict):
            cursor = cursor.get(key)
        else:
            return None
        if idx is not None:
            if not isinstance(cursor, list):
                return None
            i = int(idx)
            if i >= len(cursor):
                return None
            cursor = cursor[i]
    return cursor


def resolve_token(token: str, vars_ctx: dict[str, str], named_responses: dict[str, dict[str, Any]]) -> str:
    if token == "$guid":
        return str(uuid.uuid4())
    response_match = re.match(r"([A-Za-z0-9_\-]+)\.response\.body\.(\$.*)$", token)
    if response_match:
        name, json_path = response_match.groups()
        response_info = named_responses.get(name)
        if not response_info:
            return ""
        body = response_info.get("json")
        if body is None:
            return ""
        extracted = extract_json_path(body, json_path)
        return "" if extracted is None else str(extracted)
    return vars_ctx.get(token, "")


def interpolate(text: str, vars_ctx: dict[str, str], named_responses: dict[str, dict[str, Any]]) -> str:
    def repl(match: re.Match[str]) -> str:
        token = match.group(1)
        return resolve_token(token, vars_ctx, named_responses)

    return VAR_PATTERN.sub(repl, text)


def main() -> int:
    parser = argparse.ArgumentParser(description="Run Bosch .http suite in CI")
    parser.add_argument("--base-url", default="http://127.0.0.1:5000")
    parser.add_argument("--suite-dir", default="src/backend/Atlas.WebApi/Bosch.http")
    parser.add_argument("--summary-file", default="artifacts/api-suite-summary.json")
    parser.add_argument("--log-file", default="artifacts/api-suite-log.json")
    parser.add_argument("--failed-file", default="artifacts/api-suite-failures.json")
    args = parser.parse_args()

    vars_ctx: dict[str, str] = {
        "baseUrl": args.base_url,
        "tenantId": "00000000-0000-0000-0000-000000000001",
    }
    named_responses: dict[str, dict[str, Any]] = {}

    suite_dir = Path(args.suite_dir)
    files = sorted(suite_dir.glob("*.http"))
    if not files:
        print(f"No .http files found in {suite_dir}", file=sys.stderr)
        return 2

    requests_to_run: list[RequestBlock] = []
    for f in files:
        requests_to_run.extend(parse_http_file(f, vars_ctx))

    total = len(requests_to_run)
    passed = 0
    failed = 0
    logs: list[dict[str, Any]] = []
    failed_samples: list[dict[str, Any]] = []

    for req in requests_to_run:
        url = interpolate(req.url, vars_ctx, named_responses)
        headers = {k: interpolate(v, vars_ctx, named_responses) for k, v in req.headers.items()}
        body_str = interpolate(req.body, vars_ctx, named_responses) if req.body else ""
        body_bytes = body_str.encode("utf-8") if body_str else None

        status = 0
        raw_response = ""
        json_body: Any = None
        err = ""

        try:
            http_req = request.Request(url=url, method=req.method, headers=headers, data=body_bytes)
            with request.urlopen(http_req, timeout=60) as resp:
                status = resp.getcode()
                raw_response = resp.read().decode("utf-8", errors="replace")
        except error.HTTPError as ex:
            status = ex.code
            raw_response = ex.read().decode("utf-8", errors="replace")
            err = str(ex)
        except Exception as ex:
            err = str(ex)

        if raw_response:
            try:
                json_body = json.loads(raw_response)
            except json.JSONDecodeError:
                json_body = None

        is_ok = 200 <= status < 400
        if is_ok:
            passed += 1
        else:
            failed += 1
            failed_samples.append(
                {
                    "file": req.file,
                    "title": req.title,
                    "method": req.method,
                    "url": url,
                    "status": status,
                    "error": err,
                    "requestHeaders": headers,
                    "requestBody": body_str,
                    "responseBody": raw_response[:4000],
                }
            )

        if req.name:
            named_responses[req.name] = {
                "status": status,
                "body": raw_response,
                "json": json_body,
            }

        # 每次请求后应用可能的变量赋值语句（兼容块外赋值）
        if req.name and json_body is not None:
            vars_ctx[f"{req.name}.status"] = str(status)

        logs.append(
            {
                "file": req.file,
                "title": req.title,
                "name": req.name,
                "method": req.method,
                "url": url,
                "status": status,
                "ok": is_ok,
                "error": err,
            }
        )

    summary = {
        "total": total,
        "passed": passed,
        "failed": failed,
    }

    for out_file in [args.summary_file, args.log_file, args.failed_file]:
        Path(out_file).parent.mkdir(parents=True, exist_ok=True)

    Path(args.summary_file).write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    Path(args.log_file).write_text(json.dumps(logs, ensure_ascii=False, indent=2), encoding="utf-8")
    Path(args.failed_file).write_text(json.dumps(failed_samples, ensure_ascii=False, indent=2), encoding="utf-8")

    print(f"接口总数: {total}")
    print(f"通过数: {passed}")
    print(f"失败数: {failed}")

    if failed > 0:
        print("失败请求样例已写入:", args.failed_file)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
