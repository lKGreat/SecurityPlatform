/**
 * 审批流节点表单同步 Composable
 * 封装 syncNodeRefs / clearRefs 逻辑，减少 ApprovalPropertiesPanel 的代码量
 */
import type { Ref } from "vue";
import type {
  TreeNode,
  ConditionBranch,
  ApproveNode,
  CopyNode,
  InclusiveNode,
  RouteNode,
  CallProcessNode,
  TimerNode,
  TriggerNode,
  StartNode,
  ConditionGroup
} from "@/types/approval-tree";
import type { LfFormField } from "@/types/approval-definition";
import {
  isApproveNode,
  isCopyNode,
  isConditionBranch,
  isInclusiveNode,
  isRouteNode,
  isCallProcessNode,
  isTimerNode,
  isTriggerNode,
  isStartNode
} from "@/utils/workflow-node-guards";

interface BranchForm {
  id: string;
  branchName: string;
  isDefault?: boolean;
  conditionRule?: {
    field: string;
    operator: string;
    value: unknown;
  };
  conditionGroups?: ConditionGroup[];
  conditionExpr?: string;
}

export interface NodeFormState {
  formData: Ref<unknown>;
  approveForm: Ref<unknown>;
  copyForm: Ref<unknown>;
  branchForm: Ref<unknown>;
  inclusiveForm: Ref<unknown>;
  routeForm: Ref<unknown>;
  callProcessForm: Ref<unknown>;
  timerForm: Ref<unknown>;
  triggerForm: Ref<unknown>;
  startForm: Ref<unknown>;
  activeTab: Ref<string>;
  approverTargets: Ref<string[]>;
  assigneeExpression: Ref<string>;
  assigneeLevel: Ref<number | null>;
  noticeChannels: Ref<number[]>;
  noticeTemplateId: Ref<string | undefined>;
  formPermMap: Ref<Record<string, "R" | "E" | "H">>;
}

function extractConditionExpr(conditionRule: unknown): string | undefined {
  if (!conditionRule || typeof conditionRule !== "object") return undefined;
  const value = conditionRule as { exprType?: unknown; expression?: unknown };
  if (value.exprType === "cel" && typeof value.expression === "string") {
    return value.expression;
  }
  return undefined;
}

export function useNodeFormSync(state: NodeFormState, getFormFields: () => LfFormField[] | undefined) {
  function isPickerAssigneeType(type: ApproveNode["assigneeType"]): boolean {
    return type === 0 || type === 1;
  }

  function clearRefs() {
    state.approveForm.value = null;
    state.copyForm.value = null;
    state.branchForm.value = null;
    state.inclusiveForm.value = null;
    state.routeForm.value = null;
    state.callProcessForm.value = null;
    state.timerForm.value = null;
    state.triggerForm.value = null;
    state.startForm.value = null;
    state.activeTab.value = "approver";
    state.approverTargets.value = [];
    state.assigneeExpression.value = "";
    state.assigneeLevel.value = null;
    state.formPermMap.value = {};
    state.noticeChannels.value = [];
    state.noticeTemplateId.value = undefined;
  }

  function syncNodeRefs() {
    const current = state.formData.value as TreeNode | ConditionBranch | null;
    clearRefs();
    if (!current) return;

    if (isApproveNode(current)) {
      const node = current as ApproveNode;
      state.approveForm.value = node;

      if (isPickerAssigneeType(node.assigneeType)) {
        state.approverTargets.value = node.assigneeValue ? node.assigneeValue.split(",").filter(Boolean) : [];
        state.assigneeExpression.value = "";
        state.assigneeLevel.value = null;
      } else if (node.assigneeType === 4) {
        state.approverTargets.value = [];
        state.assigneeExpression.value = "";
        const parsedLevel = Number.parseInt(node.assigneeValue, 10);
        state.assigneeLevel.value = Number.isFinite(parsedLevel) && parsedLevel > 0 ? parsedLevel : null;
      } else {
        state.approverTargets.value = [];
        state.assigneeExpression.value = node.assigneeValue ?? "";
        state.assigneeLevel.value = null;
      }

      if (!node.voteWeight || node.voteWeight < 1) node.voteWeight = 1;
      if (!node.votePassRate || node.votePassRate < 1 || node.votePassRate > 100) node.votePassRate = 60;

      state.formPermMap.value = {};
      const formFields = getFormFields();
      if (formFields) {
        formFields.forEach((f) => {
          const fieldId = f.id || f.fieldId;
          const perm = node.formPermissionConfig?.fields?.find((p) => p.fieldId === fieldId)?.perm;
          state.formPermMap.value[fieldId] = perm ?? "E";
        });
      }

      if (node.noticeConfig) {
        state.noticeChannels.value = node.noticeConfig.channelIds || [];
        state.noticeTemplateId.value = node.noticeConfig.templateId;
      } else {
        state.noticeChannels.value = [1];
        state.noticeTemplateId.value = undefined;
      }

      if (!node.callAi) node.callAi = false;
      if (!node.aiConfig) node.aiConfig = "";

    } else if (isCopyNode(current)) {
      const node = current as CopyNode;
      state.copyForm.value = node;

      state.formPermMap.value = {};
      const formFields = getFormFields();
      if (formFields) {
        formFields.forEach((f) => {
          const fieldId = f.id || f.fieldId;
          const perm = node.formPermissionConfig?.fields?.find((p) => p.fieldId === fieldId)?.perm;
          state.formPermMap.value[fieldId] = perm ?? "R";
        });
      }

    } else if (isConditionBranch(current)) {
      let groups = current.conditionGroups ? structuredClone(current.conditionGroups) : [];
      if (groups.length === 0 && current.conditionRule) {
        groups = [{
          conditions: [{
            field: current.conditionRule.field,
            operator: current.conditionRule.operator,
            value: current.conditionRule.value
          }]
        }];
      }

      state.branchForm.value = {
        id: current.id,
        branchName: current.branchName,
        isDefault: current.isDefault,
        conditionRule: current.conditionRule
          ? { ...current.conditionRule, value: current.conditionRule.value as unknown }
          : undefined,
        conditionGroups: groups,
        conditionExpr: extractConditionExpr(current.conditionRule)
      };
    } else if (isInclusiveNode(current)) {
      state.inclusiveForm.value = current;
    } else if (isRouteNode(current)) {
      state.routeForm.value = current;
    } else if (isCallProcessNode(current)) {
      state.callProcessForm.value = current;
    } else if (isTimerNode(current)) {
      state.timerForm.value = current;
      if (!current.timerConfig) {
        current.timerConfig = { type: "duration", duration: 0 };
      }
    } else if (isTriggerNode(current)) {
      state.triggerForm.value = current;
    } else if (isStartNode(current)) {
      state.startForm.value = current;
    }
  }

  return { syncNodeRefs, clearRefs, isPickerAssigneeType };
}
