import type { NodeType, FlowNode } from "@/types/workflow";
import { nanoid } from "nanoid";

export interface PaletteItem {
  type: NodeType;
  label: string;
  description?: string;
}

export const paletteItems: PaletteItem[] = [
  { type: "start", label: "开始" },
  { type: "approve", label: "审批" },
  { type: "condition", label: "条件" },
  { type: "parallel", label: "并行" },
  { type: "parallel-join", label: "聚合" },
  { type: "copy", label: "抄送" },
  { type: "task", label: "任务" },
  { type: "end", label: "结束" }
];

export function createNode(type: NodeType, name?: string): FlowNode {
  return {
    id: nanoid(),
    type,
    name: name || defaultName(type),
    children: [],
    ext: {}
  };
}

function defaultName(type: NodeType): string {
  switch (type) {
    case "start":
      return "开始";
    case "end":
      return "结束";
    case "approve":
      return "审批";
    case "condition":
      return "条件";
    case "parallel":
      return "并行";
    case "parallel-join":
      return "聚合";
    case "copy":
      return "抄送";
    case "task":
      return "任务";
    default:
      return type;
  }
}
