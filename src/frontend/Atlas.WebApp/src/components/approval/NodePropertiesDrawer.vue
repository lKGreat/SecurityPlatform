<template>
  <a-drawer
    :open="open"
    title="节点属性"
    placement="right"
    width="400"
    @close="handleClose"
  >
    <a-form :model="formData" layout="vertical" v-if="formData">
      <a-form-item label="节点名称" v-if="'nodeName' in formData">
        <a-input v-model:value="formData.nodeName" />
      </a-form-item>
      
      <!-- 审批节点属性 -->
      <template v-if="approveNode && approverConfig">
        <a-tabs>
          <a-tab-pane key="approver" tab="审批设置">
            <a-form-item label="审批人类型">
              <a-select v-model:value="approverConfig.setType">
                <a-select-option :value="0">指定人员</a-select-option>
                <a-select-option :value="1">指定角色</a-select-option>
                <a-select-option :value="2">部门负责人</a-select-option>
                <a-select-option :value="3">HRBP</a-select-option>
                <a-select-option :value="4">直属领导</a-select-option>
                <a-select-option :value="5">层级领导</a-select-option>
                <a-select-option :value="6">发起人</a-select-option>
                <a-select-option :value="7">发起人自选</a-select-option>
              </a-select>
            </a-form-item>
            <a-form-item label="审批人列表">
              <a-select
                mode="tags"
                v-model:value="approverTargets"
                placeholder="输入人员/角色/部门ID"
                @change="syncApproverTargets"
              />
            </a-form-item>
            <a-form-item label="审批方式">
              <a-select v-model:value="approverConfig.signType">
                <a-select-option :value="1">会签</a-select-option>
                <a-select-option :value="2">或签</a-select-option>
                <a-select-option :value="3">顺序会签</a-select-option>
              </a-select>
            </a-form-item>
            <a-form-item label="无审批人策略">
              <a-select v-model:value="approverConfig.noHeaderAction">
                <a-select-option :value="0">不允许发起</a-select-option>
                <a-select-option :value="1">跳过</a-select-option>
                <a-select-option :value="2">转交管理员</a-select-option>
              </a-select>
            </a-form-item>
          </a-tab-pane>
          <a-tab-pane key="permissions" tab="扩展配置">
            <a-form-item label="按钮权限(JSON)">
              <a-textarea v-model:value="buttonPermissionText" :rows="4" />
            </a-form-item>
            <a-form-item label="表单权限(JSON)">
              <a-textarea v-model:value="formPermissionText" :rows="4" />
            </a-form-item>
            <a-form-item label="通知配置(JSON)">
              <a-textarea v-model:value="noticeConfigText" :rows="3" />
            </a-form-item>
          </a-tab-pane>
          <a-tab-pane key="legacy" tab="兼容字段">
            <a-form-item label="审批人类型(旧)">
              <a-select v-model:value="approveNode.assigneeType">
                <a-select-option :value="0">指定用户</a-select-option>
                <a-select-option :value="1">按角色</a-select-option>
                <a-select-option :value="2">部门负责人</a-select-option>
              </a-select>
            </a-form-item>
            <a-form-item label="审批人值(旧)">
              <a-input v-model:value="approveNode.assigneeValue" placeholder="用户ID/角色代码/部门ID" />
            </a-form-item>
            <a-form-item label="审批模式(旧)">
              <a-select v-model:value="approveNode.approvalMode">
                <a-select-option value="all">会签（全部通过）</a-select-option>
                <a-select-option value="any">或签（任一通过）</a-select-option>
                <a-select-option value="sequential">顺序会签</a-select-option>
              </a-select>
            </a-form-item>
          </a-tab-pane>
        </a-tabs>
      </template>

      <!-- 抄送节点属性 -->
      <template v-if="copyNode">
        <a-form-item label="抄送人">
           <a-select mode="tags" v-model:value="copyNode.copyToUsers" placeholder="输入用户ID" />
        </a-form-item>
      </template>

      <!-- 条件分支属性 -->
      <template v-if="branchNode">
         <a-form-item label="分支名称">
            <a-input v-model:value="branchNode.branchName" />
         </a-form-item>
         <a-form-item label="默认分支">
            <a-switch v-model:checked="branchNode.isDefault" />
         </a-form-item>
         <template v-if="!branchNode.isDefault">
             <a-divider>条件规则</a-divider>
             <div v-if="!branchNode.conditionRule">
                 <a-button type="dashed" block @click="initConditionRule">添加规则</a-button>
             </div>
             <div v-else>
                 <a-form-item label="字段">
                    <a-input v-model:value="branchNode.conditionRule.field" />
                 </a-form-item>
                 <a-form-item label="运算符">
                    <a-select v-model:value="branchNode.conditionRule.operator">
                        <a-select-option value="equals">等于</a-select-option>
                        <a-select-option value="notEquals">不等于</a-select-option>
                        <a-select-option value="greaterThan">大于</a-select-option>
                        <a-select-option value="lessThan">小于</a-select-option>
                        <a-select-option value="contains">包含</a-select-option>
                    </a-select>
                 </a-form-item>
                 <a-form-item label="值">
                    <a-input v-model:value="branchNode.conditionRule.value" />
                 </a-form-item>
                 <a-button type="link" danger @click="removeConditionRule">删除规则</a-button>
             </div>
         </template>
      </template>

      <a-form-item>
        <a-button type="primary" @click="handleSave">确定</a-button>
      </a-form-item>
    </a-form>
  </a-drawer>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { message } from 'ant-design-vue';
import type { TreeNode, ConditionBranch, ApproveNode, CopyNode } from '@/types/approval-tree';
import type { ButtonPermissionConfig, FormPermissionConfig, NoticeConfig } from '@/types/approval-definition';

const props = defineProps<{
  open: boolean;
  node: TreeNode | ConditionBranch | null;
}>();

const emit = defineEmits<{
  'update:open': [value: boolean];
  'update': [node: TreeNode | ConditionBranch];
}>();

const formData = ref<TreeNode | ConditionBranch | null>(null);
const approverTargets = ref<string[]>([]);
const buttonPermissionText = ref('');
const formPermissionText = ref('');
const noticeConfigText = ref('');
const approveNode = ref<ApproveNode | null>(null);
const copyNode = ref<CopyNode | null>(null);
const branchNode = ref<ConditionBranch | null>(null);
const approverConfig = computed(() => approveNode.value?.approverConfig ?? null);

watch(() => props.node, (newNode) => {
  if (newNode) {
    formData.value = structuredClone(newNode);
    syncNodeRefs();
    if (approveNode.value) {
      ensureApproverConfig();
      approverTargets.value = (approveNode.value.approverConfig?.nodeApproveList ?? []).map((item) => item.targetId);
      buttonPermissionText.value = approveNode.value.buttonPermissionConfig
        ? JSON.stringify(approveNode.value.buttonPermissionConfig, null, 2)
        : '';
      formPermissionText.value = approveNode.value.formPermissionConfig
        ? JSON.stringify(approveNode.value.formPermissionConfig, null, 2)
        : '';
      noticeConfigText.value = approveNode.value.noticeConfig
        ? JSON.stringify(approveNode.value.noticeConfig, null, 2)
        : '';
    }
  } else {
    formData.value = null;
    syncNodeRefs();
  }
}, { immediate: true });

const handleClose = () => {
  emit('update:open', false);
};

const handleSave = () => {
  if (formData.value) {
    if (isApproveNode(formData.value)) {
      syncApproverTargets();
      if (!applyExtraConfigs()) {
        return;
      }
    }
    emit('update', formData.value);
    handleClose();
  }
};

const ensureApproverConfig = () => {
  const current = formData.value;
  if (!current || !isApproveNode(current)) return;
  if (!current.approverConfig) {
    current.approverConfig = {
      setType: current.assigneeType ?? 0,
      signType: current.approvalMode === 'sequential' ? 3 : current.approvalMode === 'any' ? 2 : 1,
      noHeaderAction: 0,
      nodeApproveList: current.assigneeValue
        ? [{ targetId: current.assigneeValue, name: current.assigneeValue }]
        : []
    };
  }
};

const syncApproverTargets = () => {
  const current = formData.value;
  if (!current || !isApproveNode(current)) return;
  if (!current.approverConfig) return;
  current.approverConfig.nodeApproveList = approverTargets.value.map((targetId) => ({
    targetId,
    name: targetId
  }));
};

const applyExtraConfigs = () => {
  const current = formData.value;
  if (!current || !isApproveNode(current)) return false;

  const parseJson = <T>(text: string, label: string): T | null => {
    if (!text.trim()) return null;
    try {
      return JSON.parse(text) as T;
    } catch {
      message.error(`${label}JSON格式不正确`);
      return null;
    }
  };

  const buttonConfig = parseJson<ButtonPermissionConfig>(buttonPermissionText.value, '按钮权限');
  if (buttonConfig === null) return false;
  const formPermConfig = parseJson<FormPermissionConfig>(formPermissionText.value, '表单权限');
  if (formPermConfig === null) return false;
  const noticeConfig = parseJson<NoticeConfig>(noticeConfigText.value, '通知配置');
  if (noticeConfig === null) return false;

  current.buttonPermissionConfig = buttonConfig;
  current.formPermissionConfig = formPermConfig;
  current.noticeConfig = noticeConfig;
  return true;
};

const isApproveNode = (node: TreeNode | ConditionBranch): node is ApproveNode => {
  return 'nodeType' in node && node.nodeType === 'approve';
};

const isCopyNode = (node: TreeNode | ConditionBranch): node is CopyNode => {
  return 'nodeType' in node && node.nodeType === 'copy';
};

const isConditionBranch = (node: TreeNode | ConditionBranch): node is ConditionBranch => {
  return 'branchName' in node;
};

const syncNodeRefs = () => {
  const current = formData.value;
  approveNode.value = current && isApproveNode(current) ? current : null;
  copyNode.value = current && isCopyNode(current) ? current : null;
  branchNode.value = current && isConditionBranch(current) ? current : null;
};
const initConditionRule = () => {
  const current = formData.value;
  if (!current || !isConditionBranch(current)) return;
  current.conditionRule = {
    field: '',
    operator: 'equals',
    value: ''
  };
};

const removeConditionRule = () => {
  const current = formData.value;
  if (!current || !isConditionBranch(current)) return;
  current.conditionRule = undefined;
};
</script>
