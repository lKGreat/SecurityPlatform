/**
 * useApprovalTree composable — 兼容层
 *
 * 保留原有 API 签名，内部代理到 Pinia Store（useApprovalFlowStore）。
 * 已有消费方（ApprovalDesignerPage.vue 等）无需修改即可正常工作。
 */
import { computed, toRef } from 'vue';
import { storeToRefs } from 'pinia';
import { useApprovalFlowStore } from '@/stores/approvalFlow';
import type {
  ApprovalFlowTree,
  TreeNode,
  ConditionBranch,
} from '@/types/approval-tree';
import type { ApprovalTreeValidationIssue } from '@/utils/approval-tree-validator';

export function useApprovalTree() {
  const store = useApprovalFlowStore();

  // Reactive refs that mirror the store state
  const flowTree = computed({
    get: () => store.flowTree,
    set: (val: ApprovalFlowTree) => {
      store.initFlowTree(val);
    },
  });

  const selectedNode = computed({
    get: () => store.selectedNode,
    set: (val: TreeNode | ConditionBranch | null) => {
      store.selectNode(val?.id ?? null);
    },
  });

  const canUndo = computed(() => store.canUndo);
  const canRedo = computed(() => store.canRedo);

  // Delegate to store actions
  const addNode = (parentId: string, newNodeType: string) => {
    store.addNode(parentId, newNodeType);
  };

  const deleteNode = (nodeId: string) => {
    store.deleteNode(nodeId);
  };

  const updateNode = (updatedNode: TreeNode | ConditionBranch) => {
    store.updateNode(updatedNode);
  };

  const addConditionBranch = (conditionNodeId: string) => {
    store.addConditionBranch(conditionNodeId);
  };

  const deleteConditionBranch = (branchId: string) => {
    store.deleteConditionBranch(branchId);
  };

  const moveBranch = (conditionNodeId: string, branchId: string, direction: 'left' | 'right') => {
    store.moveBranch(conditionNodeId, branchId, direction);
  };

  const selectNode = (target: TreeNode | ConditionBranch | null) => {
    store.selectNode(target?.id ?? null);
  };

  const validateFlow = (): {
    valid: boolean;
    errors: string[];
    issues: ApprovalTreeValidationIssue[];
  } => {
    return store.validateFlow();
  };

  const undo = () => {
    store.undo();
  };

  const redo = () => {
    store.redo();
  };

  /**
   * pushState — 兼容层：旧代码中直接调用 pushState 推入快照。
   * 在新架构中，Store action 内部已自动推栈，此方法仅在外部直接修改
   * flowTree 后需要手动推栈时使用。
   */
  const pushState = (_state?: ApprovalFlowTree) => {
    store._pushHistory();
  };

  return {
    flowTree,
    selectedNode,
    addNode,
    deleteNode,
    updateNode,
    addConditionBranch,
    deleteConditionBranch,
    moveBranch,
    selectNode,
    validateFlow,
    undo,
    redo,
    canUndo,
    canRedo,
    pushState,
  };
}
