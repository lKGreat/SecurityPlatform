/**
 * 审批流节点类型守卫工具函数
 */
import type {
  TreeNode,
  ConditionBranch,
  ApproveNode,
  CopyNode,
  StartNode,
  InclusiveNode,
  RouteNode,
  CallProcessNode,
  TimerNode,
  TriggerNode
} from "@/types/approval-tree";

export function isApproveNode(n: TreeNode | ConditionBranch): n is ApproveNode {
  return "nodeType" in n && n.nodeType === "approve";
}

export function isCopyNode(n: TreeNode | ConditionBranch): n is CopyNode {
  return "nodeType" in n && n.nodeType === "copy";
}

export function isStartNode(n: TreeNode | ConditionBranch): n is StartNode {
  return "nodeType" in n && n.nodeType === "start";
}

export function isConditionBranch(n: TreeNode | ConditionBranch): n is ConditionBranch {
  return "branchName" in n;
}

export function isInclusiveNode(n: TreeNode | ConditionBranch): n is InclusiveNode {
  return "nodeType" in n && n.nodeType === "inclusive";
}

export function isRouteNode(n: TreeNode | ConditionBranch): n is RouteNode {
  return "nodeType" in n && n.nodeType === "route";
}

export function isCallProcessNode(n: TreeNode | ConditionBranch): n is CallProcessNode {
  return "nodeType" in n && n.nodeType === "callProcess";
}

export function isTimerNode(n: TreeNode | ConditionBranch): n is TimerNode {
  return "nodeType" in n && n.nodeType === "timer";
}

export function isTriggerNode(n: TreeNode | ConditionBranch): n is TriggerNode {
  return "nodeType" in n && n.nodeType === "trigger";
}
