/**
 * 工作流设计器时间/日期值标准化工具函数
 */

export function pad2(n: number): string {
  return n.toString().padStart(2, "0");
}

export function formatTimeSpan(totalSeconds: number): string {
  const sign = totalSeconds < 0 ? "-" : "";
  const abs = Math.abs(Math.floor(totalSeconds));
  const days = Math.floor(abs / 86400);
  const hours = Math.floor((abs % 86400) / 3600);
  const minutes = Math.floor((abs % 3600) / 60);
  const seconds = abs % 60;

  // .NET TimeSpan 支持 "d.hh:mm:ss"；当天数为 0 时用 "HH:mm:ss"
  if (days > 0) {
    return `${sign}${days}.${pad2(hours)}:${pad2(minutes)}:${pad2(seconds)}`;
  }
  return `${sign}${pad2(hours)}:${pad2(minutes)}:${pad2(seconds)}`;
}

export function normalizeTimeSpanInput(value: unknown): unknown {
  if (typeof value !== "string") return value;
  const raw = value.trim();
  if (!raw) return value;

  // 已是 HH:mm:ss / H:mm:ss
  if (/^\d{1,2}:\d{2}:\d{2}$/.test(raw)) return raw;
  // 允许 HH:mm（补秒）
  if (/^\d{1,2}:\d{2}$/.test(raw)) return `${raw}:00`;

  // 允许 5s / 10m / 2h / 1d / 1500ms
  const m = raw.match(/^(\d+(?:\.\d+)?)\s*(ms|s|m|h|d)$/i);
  if (!m) return value;
  const num = Number(m[1]);
  if (Number.isNaN(num)) return value;

  const unit = m[2].toLowerCase();
  const seconds =
    unit === "ms"
      ? Math.round(num / 1000)
      : unit === "s"
        ? Math.round(num)
        : unit === "m"
          ? Math.round(num * 60)
          : unit === "h"
            ? Math.round(num * 3600)
            : Math.round(num * 86400);

  return formatTimeSpan(seconds);
}

export function normalizeDateTimeInput(value: unknown): unknown {
  if (typeof value !== "string") return value;
  const raw = value.trim();
  if (!raw) return value;

  // ISO（带 T 或带时区）直接放行
  if (/^\d{4}-\d{2}-\d{2}T/.test(raw) || /[zZ]|[+\-]\d{2}:\d{2}$/.test(raw)) {
    return raw;
  }

  // 支持 "YYYY-MM-DD HH:mm:ss" / "YYYY-MM-DD HH:mm" / "YYYY-MM-DD"
  const m =
    raw.match(/^(\d{4})-(\d{2})-(\d{2})\s+(\d{2}):(\d{2}):(\d{2})$/) ||
    raw.match(/^(\d{4})-(\d{2})-(\d{2})\s+(\d{2}):(\d{2})$/) ||
    raw.match(/^(\d{4})-(\d{2})-(\d{2})$/);
  if (!m) return value;

  const y = m[1];
  const mo = m[2];
  const d = m[3];
  const hh = m[4] ?? "00";
  const mm = m[5] ?? "00";
  const ss = m[6] ?? "00";

  // 输出为 "YYYY-MM-DDTHH:mm:ss"（后端 System.Text.Json 可解析）
  return `${y}-${mo}-${d}T${hh}:${mm}:${ss}`;
}

export function normalizeJsonValue(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value.map((it) => normalizeJsonValue(it));
  }
  if (value && typeof value === "object") {
    const obj = value as Record<string, unknown>;
    const next: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(obj)) {
      next[k] = normalizeJsonValue(normalizeDateTimeInput(v));
    }
    return next;
  }
  // 仅 datetime 在这里做全局归一；timespan 只在步骤参数里按类型归一
  return normalizeDateTimeInput(value);
}

export function formatLocalIsoSeconds(d: Date): string {
  const yyyy = d.getFullYear();
  const mm = pad2(d.getMonth() + 1);
  const dd = pad2(d.getDate());
  const hh = pad2(d.getHours());
  const mi = pad2(d.getMinutes());
  const ss = pad2(d.getSeconds());
  return `${yyyy}-${mm}-${dd}T${hh}:${mi}:${ss}`;
}
