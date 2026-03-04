/**
 * 审批流相关常量枚举
 */
import type { ApproveNode } from "@/types/approval-tree";

export const ASSIGNEE_TYPE_OPTIONS: Array<{ value: ApproveNode["assigneeType"]; label: string }> = [
  { value: 0, label: "指定人员" },
  { value: 1, label: "指定角色" },
  { value: 2, label: "部门负责人" },
  { value: 3, label: "逐级领导（Loop）" },
  { value: 4, label: "指定层级（Level）" },
  { value: 5, label: "直属领导" },
  { value: 6, label: "发起人" },
  { value: 7, label: "HRBP" },
  { value: 8, label: "发起人自选" },
  { value: 9, label: "业务字段取人" },
  { value: 10, label: "外部传入人员" }
];

export type AssigneeType = ApproveNode["assigneeType"];

/** 使用人员/角色选择器的审批人类型 */
export const PICKER_ASSIGNEE_TYPES: Set<AssigneeType> = new Set([0, 1]);
