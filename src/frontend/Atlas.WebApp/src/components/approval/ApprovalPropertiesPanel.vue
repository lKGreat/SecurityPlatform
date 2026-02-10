<template>
  <div class="dd-props-panel" :class="{ 'is-open': open }">
    <!-- 面板头部 -->
    <div class="dd-props-header" v-if="formData">
      <div class="dd-props-header__info">
        <div class="dd-props-header__icon" :class="iconClass">
          <component :is="nodeIcon" />
        </div>
        <a-input
          v-if="'nodeName' in formData"
          v-model:value="formData.nodeName"
          class="dd-props-header__name"
          :bordered="false"
          placeholder="节点名称"
        />
        <a-input
          v-else-if="'branchName' in formData"
          v-model:value="(formData as BranchForm).branchName"
          class="dd-props-header__name"
          :bordered="false"
          placeholder="分支名称"
        />
      </div>
      <button class="dd-props-header__close" @click="handleClose">
        <CloseOutlined />
      </button>
    </div>

    <!-- 面板内容 -->
    <div class="dd-props-body" v-if="formData">
      <!-- ═══ 审批节点 ═══ -->
      <template v-if="approveForm">
        <a-tabs v-model:activeKey="activeTab" size="small">
          <a-tab-pane key="approver" tab="审批设置">
            <a-form layout="vertical" class="dd-props-form">
              <a-form-item label="审批人类型">
                <a-select v-model:value="approverConfig.setType" @change="onApproverTypeChange">
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

              <a-form-item
                label="审批人"
                v-if="approverConfig.setType <= 1"
              >
                <a-select
                  mode="tags"
                  v-model:value="approverTargets"
                  :placeholder="approverConfig.setType === 0 ? '输入人员ID' : '输入角色代码'"
                  :token-separators="[',', ' ']"
                >
                </a-select>
              </a-form-item>

              <a-form-item label="多人审批方式">
                <a-radio-group v-model:value="approverConfig.signType">
                  <a-radio :value="1">
                    <span class="dd-radio-label">会签</span>
                    <span class="dd-radio-desc">需所有审批人同意</span>
                  </a-radio>
                  <a-radio :value="2">
                    <span class="dd-radio-label">或签</span>
                    <span class="dd-radio-desc">一人同意即可</span>
                  </a-radio>
                  <a-radio :value="3">
                    <span class="dd-radio-label">顺序签</span>
                    <span class="dd-radio-desc">按顺序依次审批</span>
                  </a-radio>
                </a-radio-group>
              </a-form-item>

              <a-form-item label="审批人为空时">
                <a-select v-model:value="approverConfig.noHeaderAction">
                  <a-select-option :value="0">不允许发起</a-select-option>
                  <a-select-option :value="1">自动跳过</a-select-option>
                  <a-select-option :value="2">转交管理员</a-select-option>
                </a-select>
              </a-form-item>
            </a-form>
          </a-tab-pane>

          <a-tab-pane key="form-perm" tab="表单权限">
            <a-form layout="vertical" class="dd-props-form">
              <a-form-item label="表单权限 (JSON)">
                <a-textarea
                  v-model:value="formPermissionText"
                  :rows="6"
                  placeholder='{"fields": [{"fieldId": "xxx", "perm": "R"}]}'
                />
              </a-form-item>
            </a-form>
          </a-tab-pane>

          <a-tab-pane key="btn-perm" tab="操作权限">
            <a-form layout="vertical" class="dd-props-form">
              <a-form-item label="按钮权限 (JSON)">
                <a-textarea
                  v-model:value="buttonPermissionText"
                  :rows="6"
                  placeholder='{"startPage": [1,2], "approvalPage": [1,2,3]}'
                />
              </a-form-item>
            </a-form>
          </a-tab-pane>

          <a-tab-pane key="notice" tab="通知设置">
            <a-form layout="vertical" class="dd-props-form">
              <a-form-item label="通知配置 (JSON)">
                <a-textarea
                  v-model:value="noticeConfigText"
                  :rows="4"
                  placeholder='{"channelIds": [1], "templateId": "tpl-001"}'
                />
              </a-form-item>
            </a-form>
          </a-tab-pane>
        </a-tabs>
      </template>

      <!-- ═══ 抄送节点 ═══ -->
      <template v-else-if="copyForm">
        <a-form layout="vertical" class="dd-props-form">
          <a-form-item label="抄送人">
            <a-select
              mode="tags"
              v-model:value="copyForm.copyToUsers"
              placeholder="输入用户ID"
              :token-separators="[',', ' ']"
            />
          </a-form-item>
          <a-form-item label="表单权限 (JSON)">
            <a-textarea
              v-model:value="formPermissionText"
              :rows="4"
              placeholder='{"fields": [{"fieldId": "xxx", "perm": "R"}]}'
            />
          </a-form-item>
        </a-form>
      </template>

      <!-- ═══ 条件分支 ═══ -->
      <template v-else-if="branchForm">
        <a-form layout="vertical" class="dd-props-form">
          <a-form-item label="是否默认分支">
            <a-switch v-model:checked="branchForm.isDefault" />
            <span class="dd-switch-hint" v-if="branchForm.isDefault">
              其他条件均不满足时，默认走此分支
            </span>
          </a-form-item>

          <template v-if="!branchForm.isDefault">
            <a-divider>条件规则</a-divider>
            <div v-if="!branchForm.conditionRule" class="dd-empty-rule">
              <a-button type="dashed" block @click="initConditionRule">
                <PlusOutlined /> 添加条件规则
              </a-button>
            </div>
            <template v-else>
              <a-form-item label="字段名称">
                <a-input
                  v-model:value="branchForm.conditionRule.field"
                  placeholder="如 amount, department"
                />
              </a-form-item>
              <a-form-item label="运算符">
                <a-select v-model:value="branchForm.conditionRule.operator">
                  <a-select-option value="equals">等于</a-select-option>
                  <a-select-option value="notEquals">不等于</a-select-option>
                  <a-select-option value="greaterThan">大于</a-select-option>
                  <a-select-option value="lessThan">小于</a-select-option>
                  <a-select-option value="greaterThanOrEqual">大于等于</a-select-option>
                  <a-select-option value="lessThanOrEqual">小于等于</a-select-option>
                  <a-select-option value="contains">包含</a-select-option>
                  <a-select-option value="in">在列表中</a-select-option>
                  <a-select-option value="startsWith">开头是</a-select-option>
                  <a-select-option value="endsWith">结尾是</a-select-option>
                </a-select>
              </a-form-item>
              <a-form-item label="比较值">
                <a-input
                  v-model:value="branchForm.conditionRule.value"
                  placeholder="请输入比较值"
                />
              </a-form-item>
              <a-button type="link" danger size="small" @click="removeConditionRule">
                <DeleteOutlined /> 删除规则
              </a-button>
            </template>
          </template>
        </a-form>
      </template>

      <!-- ═══ 发起人节点 ═══ -->
      <template v-else-if="startForm">
        <a-form layout="vertical" class="dd-props-form">
          <a-form-item label="发起条件">
            <a-alert message="所有人都可以作为发起人" type="info" show-icon />
          </a-form-item>
        </a-form>
      </template>
    </div>

    <!-- 底部按钮 -->
    <div class="dd-props-footer" v-if="formData">
      <a-button type="primary" block @click="handleSave">确 定</a-button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import {
  CloseOutlined,
  PlusOutlined,
  DeleteOutlined,
  UserOutlined,
  SendOutlined,
  BranchesOutlined,
  PlayCircleOutlined,
} from '@ant-design/icons-vue';
import { message } from 'ant-design-vue';
import type { TreeNode, ConditionBranch, ApproveNode, CopyNode, StartNode } from '@/types/approval-tree';
import type { ApproverConfig } from '@/types/approval-definition';

// ── 内部类型 ──
interface BranchForm {
  id: string;
  branchName: string;
  isDefault?: boolean;
  conditionRule?: {
    field: string;
    operator: string;
    value: unknown;
  };
}

// ── Props / Emits ──
const props = defineProps<{
  open: boolean;
  node: TreeNode | ConditionBranch | null;
}>();

const emit = defineEmits<{
  'update:open': [value: boolean];
  update: [node: TreeNode | ConditionBranch];
}>();

// ── 状态 ──
// Use 'any' for the cloned form data to avoid excessively deep type instantiation
// caused by the recursive TreeNode union type with structuredClone.
const formData = ref<any>(null);
const approveForm = ref<any>(null);
const copyForm = ref<any>(null);
const branchForm = ref<any>(null);
const startForm = ref<any>(null);
const activeTab = ref('approver');

const approverConfig = ref<ApproverConfig>({
  setType: 0,
  signType: 1,
  noHeaderAction: 0,
  nodeApproveList: [],
});
const approverTargets = ref<string[]>([]);
const buttonPermissionText = ref('');
const formPermissionText = ref('');
const noticeConfigText = ref('');

// ── 计算属性 ──
const iconClass = computed(() => {
  if (approveForm.value) return 'dd-props-header__icon--approve';
  if (copyForm.value) return 'dd-props-header__icon--copy';
  if (branchForm.value) return 'dd-props-header__icon--condition';
  return 'dd-props-header__icon--start';
});

const nodeIcon = computed(() => {
  if (approveForm.value) return UserOutlined;
  if (copyForm.value) return SendOutlined;
  if (branchForm.value) return BranchesOutlined;
  return PlayCircleOutlined;
});

// ── Watch props.node ──
watch(
  () => props.node,
  (newNode) => {
    if (newNode) {
      formData.value = structuredClone(newNode);
      syncNodeRefs();
    } else {
      formData.value = null;
      clearRefs();
    }
  },
  { immediate: true },
);

function syncNodeRefs() {
  const current = formData.value;
  clearRefs();

  if (!current) return;

  if (isApproveNode(current)) {
    const node = current as ApproveNode;
    approveForm.value = node;
    ensureApproverConfig();
    const list = approveForm.value?.approverConfig?.nodeApproveList as Array<{ targetId: string; name: string }> | undefined;
    approverTargets.value = (list ?? []).map((item) => item.targetId);
    buttonPermissionText.value = node.buttonPermissionConfig
      ? JSON.stringify(node.buttonPermissionConfig, null, 2)
      : '';
    formPermissionText.value = node.formPermissionConfig
      ? JSON.stringify(node.formPermissionConfig, null, 2)
      : '';
    noticeConfigText.value = node.noticeConfig
      ? JSON.stringify(node.noticeConfig, null, 2)
      : '';
  } else if (isCopyNode(current)) {
    const node = current as CopyNode;
    copyForm.value = node;
    formPermissionText.value = node.formPermissionConfig
      ? JSON.stringify(node.formPermissionConfig, null, 2)
      : '';
  } else if (isConditionBranch(current)) {
    branchForm.value = {
      id: current.id,
      branchName: current.branchName,
      isDefault: current.isDefault,
      conditionRule: current.conditionRule
        ? { ...current.conditionRule, value: current.conditionRule.value as unknown }
        : undefined,
    };
  } else if (isStartNode(current)) {
    startForm.value = current;
  }
}

function clearRefs() {
  approveForm.value = null;
  copyForm.value = null;
  branchForm.value = null;
  startForm.value = null;
  activeTab.value = 'approver';
}

function ensureApproverConfig() {
  if (!approveForm.value) return;
  if (!approveForm.value.approverConfig) {
    approveForm.value.approverConfig = {
      setType: approveForm.value.assigneeType ?? 0,
      signType:
        approveForm.value.approvalMode === 'sequential'
          ? 3
          : approveForm.value.approvalMode === 'any'
            ? 2
            : 1,
      noHeaderAction: 0,
      nodeApproveList: approveForm.value.assigneeValue
        ? [{ targetId: approveForm.value.assigneeValue, name: approveForm.value.assigneeValue }]
        : [],
    };
  }
  approverConfig.value = approveForm.value.approverConfig;
}

// ── Event handlers ──
function onApproverTypeChange() {
  // 切换类型时清空审批人列表
  approverTargets.value = [];
}

function handleClose() {
  emit('update:open', false);
}

function handleSave() {
  if (!formData.value) return;

  if (approveForm.value) {
    // 同步 targets
    approverConfig.value.nodeApproveList = approverTargets.value.map((id) => ({
      targetId: id,
      name: id,
    }));
    // 同步旧字段
    approveForm.value.assigneeType = approverConfig.value.setType as ApproveNode['assigneeType'];
    approveForm.value.assigneeValue = approverTargets.value[0] ?? '';
    approveForm.value.approvalMode =
      approverConfig.value.signType === 3
        ? 'sequential'
        : approverConfig.value.signType === 2
          ? 'any'
          : 'all';

    // 解析 JSON 配置
    if (!applyJsonConfig(buttonPermissionText.value, '按钮权限', (v) => {
      approveForm.value!.buttonPermissionConfig = v as ApproveNode['buttonPermissionConfig'];
    })) return;
    if (!applyJsonConfig(formPermissionText.value, '表单权限', (v) => {
      approveForm.value!.formPermissionConfig = v as ApproveNode['formPermissionConfig'];
    })) return;
    if (!applyJsonConfig(noticeConfigText.value, '通知配置', (v) => {
      approveForm.value!.noticeConfig = v as ApproveNode['noticeConfig'];
    })) return;
  }

  if (copyForm.value) {
    if (!applyJsonConfig(formPermissionText.value, '表单权限', (v) => {
      copyForm.value!.formPermissionConfig = v as CopyNode['formPermissionConfig'];
    })) return;
  }

  // 如果是分支，合并回 formData
  if (branchForm.value) {
    const branch = formData.value as ConditionBranch;
    branch.branchName = branchForm.value.branchName;
    branch.isDefault = branchForm.value.isDefault;
    branch.conditionRule = branchForm.value.conditionRule as ConditionBranch['conditionRule'];
  }

  emit('update', formData.value);
  handleClose();
}

function applyJsonConfig(text: string, label: string, setter: (v: unknown) => void): boolean {
  if (!text.trim()) {
    setter(undefined);
    return true;
  }
  try {
    setter(JSON.parse(text));
    return true;
  } catch {
    message.error(`${label} JSON 格式不正确`);
    return false;
  }
}

function initConditionRule() {
  if (!branchForm.value) return;
  branchForm.value.conditionRule = {
    field: '',
    operator: 'equals',
    value: '',
  };
}

function removeConditionRule() {
  if (!branchForm.value) return;
  branchForm.value.conditionRule = undefined;
}

// ── 类型守卫 ──
function isApproveNode(n: TreeNode | ConditionBranch): n is ApproveNode {
  return 'nodeType' in n && n.nodeType === 'approve';
}
function isCopyNode(n: TreeNode | ConditionBranch): n is CopyNode {
  return 'nodeType' in n && n.nodeType === 'copy';
}
function isStartNode(n: TreeNode | ConditionBranch): n is StartNode {
  return 'nodeType' in n && n.nodeType === 'start';
}
function isConditionBranch(n: TreeNode | ConditionBranch): n is ConditionBranch {
  return 'branchName' in n;
}
</script>

<style scoped>
.dd-props-panel {
  position: absolute;
  right: 0;
  top: 0;
  bottom: 0;
  width: 0;
  background: #fff;
  box-shadow: -4px 0 12px rgba(0, 0, 0, 0.08);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  transition: width 0.3s cubic-bezier(0.645, 0.045, 0.355, 1);
  z-index: 20;
}

.dd-props-panel.is-open {
  width: 400px;
}

/* ── Header ── */
.dd-props-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 16px 12px;
  border-bottom: 1px solid #f0f0f0;
  flex-shrink: 0;
}

.dd-props-header__info {
  display: flex;
  align-items: center;
  gap: 8px;
  flex: 1;
  min-width: 0;
}

.dd-props-header__icon {
  width: 32px;
  height: 32px;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  font-size: 16px;
  flex-shrink: 0;
}

.dd-props-header__icon--approve {
  background: #ff943e;
}
.dd-props-header__icon--copy {
  background: #3296fa;
}
.dd-props-header__icon--condition {
  background: #15bc83;
}
.dd-props-header__icon--start {
  background: #576a95;
}

.dd-props-header__name {
  font-size: 16px;
  font-weight: 600;
  padding: 0;
}

.dd-props-header__close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: none;
  background: transparent;
  cursor: pointer;
  border-radius: 4px;
  color: #8c8c8c;
  font-size: 14px;
  flex-shrink: 0;
}

.dd-props-header__close:hover {
  background: #f5f5f5;
  color: #1a1a1a;
}

/* ── Body ── */
.dd-props-body {
  flex: 1;
  overflow-y: auto;
  padding: 12px 16px;
}

.dd-props-form {
  padding: 4px 0;
}

/* ── 审批方式 radio 样式 ── */
.dd-props-form :deep(.ant-radio-wrapper) {
  display: flex;
  align-items: flex-start;
  padding: 8px 0;
}

.dd-radio-label {
  font-weight: 500;
  display: block;
}

.dd-radio-desc {
  display: block;
  font-size: 12px;
  color: #8c8c8c;
  margin-top: 2px;
}

.dd-switch-hint {
  display: inline-block;
  margin-left: 8px;
  font-size: 12px;
  color: #8c8c8c;
}

.dd-empty-rule {
  padding: 8px 0;
}

/* ── Footer ── */
.dd-props-footer {
  padding: 12px 16px;
  border-top: 1px solid #f0f0f0;
  flex-shrink: 0;
}
</style>
