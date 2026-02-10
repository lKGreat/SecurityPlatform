<template>
  <a-card class="page-card">
    <template #title>
      <a-input
        v-model:value="flowName"
        placeholder="流程名称"
        style="width: 320px"
        :maxlength="100"
      />
    </template>
    <template #extra>
      <a-space>
        <a-button v-if="activeStep === 2" @click="undo" :disabled="!canUndo">
          <UndoOutlined /> 撤销
        </a-button>
        <a-button v-if="activeStep === 2" @click="redo" :disabled="!canRedo">
          <RedoOutlined /> 重做
        </a-button>
        <a-button @click="handleSave">保存</a-button>
        <a-button type="primary" @click="handlePublish">发布</a-button>
      </a-space>
    </template>

    <a-steps class="designer-steps" :current="activeStep">
      <a-step title="基础设置" />
      <a-step title="表单设计" />
      <a-step title="流程设计" />
    </a-steps>

    <div class="step-content" v-show="activeStep === 0">
      <a-form layout="vertical" class="basic-form">
        <a-form-item label="流程名称">
          <a-input v-model:value="flowName" :maxlength="100" placeholder="请输入流程名称" />
        </a-form-item>
        <a-form-item label="流程分类">
          <a-input v-model:value="definitionMeta.category" placeholder="如：采购/人事/财务" />
        </a-form-item>
        <a-form-item label="流程说明">
          <a-textarea v-model:value="definitionMeta.description" :rows="3" />
        </a-form-item>
        <a-form-item label="可见范围(JSON)">
          <a-textarea
            v-model:value="visibilityScopeText"
            :rows="3"
            placeholder='{"scopeType":"All"}'
          />
        </a-form-item>
        <a-space>
          <a-switch v-model:checked="definitionMeta.isQuickEntry" /> <span>快捷入口</span>
          <a-switch v-model:checked="definitionMeta.isLowCodeFlow" /> <span>启用低代码表单</span>
        </a-space>
      </a-form>
    </div>

    <div class="step-content" v-show="activeStep === 1">
      <LfFormDesigner
        v-model="lfFormModel"
        @update:formFields="handleLfFormFields"
      />
    </div>
    
    <div class="designer-container" v-show="activeStep === 2">
      <X6ApprovalDesigner
        :flow-tree="flowTree"
        :selected-node-id="selectedNode?.id ?? null"
        @selectNode="handleSelectNode"
        @addNode="addNode"
        @deleteNode="deleteNode"
        @addConditionBranch="addConditionBranch"
        @deleteConditionBranch="deleteConditionBranch"
      />

      <ApprovalPropertiesPanel
        :open="panelOpen"
        :node="selectedNode"
        @update:open="panelOpen = $event"
        @update="handleNodeUpdate"
      />
    </div>

    <div class="step-actions">
      <a-space>
        <a-button @click="prevStep" :disabled="activeStep === 0">上一步</a-button>
        <a-button @click="nextStep" :disabled="activeStep === 2">下一步</a-button>
      </a-space>
    </div>
  </a-card>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { message } from 'ant-design-vue';
import { UndoOutlined, RedoOutlined } from '@ant-design/icons-vue';
import X6ApprovalDesigner from '@/components/approval/x6/X6ApprovalDesigner.vue';
import ApprovalPropertiesPanel from '@/components/approval/ApprovalPropertiesPanel.vue';
import LfFormDesigner from '@/components/approval/LfFormDesigner.vue';
import { useApprovalTree } from '@/composables/useApprovalTree';
import { ApprovalTreeConverter } from '@/utils/approval-tree-converter';
import type { ApprovalDefinitionMeta, LfFormPayload, FormJson, VisibilityScope } from '@/types/approval-definition';
import type { TreeNode, ConditionBranch } from '@/types/approval-tree';
import {
  getApprovalFlowById,
  createApprovalFlow,
  updateApprovalFlow,
  publishApprovalFlow
} from '@/services/api';

const route = useRoute();
const router = useRouter();

const { 
    flowTree, 
    selectedNode, 
    addNode, 
    deleteNode, 
    updateNode, 
    addConditionBranch, 
    deleteConditionBranch, 
    selectNode, 
    validateFlow,
    undo,
    redo,
    canUndo,
    canRedo,
    pushState
} = useApprovalTree();

const flowName = ref('');
const flowId = ref<string | null>(null);
const panelOpen = ref(false);
const activeStep = ref(0);
const definitionMeta = ref<ApprovalDefinitionMeta>({
  flowName: '',
  isLowCodeFlow: true
});
const lfFormPayload = ref<LfFormPayload | undefined>(undefined);
const lfFormModel = ref<FormJson | undefined>(undefined);
const visibilityScopeText = ref('');

// 节点选中时打开面板
watch(selectedNode, (node) => {
  panelOpen.value = !!node;
});

const handleSelectNode = (node: TreeNode | ConditionBranch | null) => {
  selectNode(node);
};

const handleNodeUpdate = (updatedNode: TreeNode | ConditionBranch) => {
    updateNode(updatedNode);
};

const nextStep = () => {
  if (activeStep.value < 2) {
    activeStep.value += 1;
  }
};

const prevStep = () => {
  if (activeStep.value > 0) {
    activeStep.value -= 1;
  }
};

const handleLfFormFields = (fields: LfFormPayload['formFields']) => {
  lfFormPayload.value = {
    formJson: lfFormModel.value ?? { widgetList: [] },
    formFields: fields
  };
};

const loadFlow = async () => {
  const id = route.params.id as string;
  if (!id || id === 'undefined') {
      pushState(flowTree.value);
      return;
  }
  
  try {
    const flow = await getApprovalFlowById(id);
    flowName.value = flow.name;
    flowId.value = flow.id;
    definitionMeta.value.description = flow.description;
    definitionMeta.value.category = flow.category;
    definitionMeta.value.isQuickEntry = flow.isQuickEntry;
    if (flow.visibilityScopeJson && !definitionMeta.value.visibilityScope) {
      visibilityScopeText.value = flow.visibilityScopeJson;
      const parsedScope = parseVisibilityScope(flow.visibilityScopeJson);
      definitionMeta.value.visibilityScope = parsedScope ?? undefined;
    }
    
    if (flow.definitionJson) {
      const state = ApprovalTreeConverter.definitionJsonToState(flow.definitionJson);
      flowTree.value = state.tree;
      if (state.meta) {
        definitionMeta.value = state.meta;
        visibilityScopeText.value = state.meta.visibilityScope
          ? JSON.stringify(state.meta.visibilityScope, null, 2)
          : '';
      }
      if (state.lfForm) {
        lfFormPayload.value = state.lfForm;
        lfFormModel.value = state.lfForm.formJson;
      }
    }
    
    pushState(flowTree.value);
  } catch (err) {
    message.error(err instanceof Error ? err.message : '加载失败');
  }
};

const handleSave = async () => {
  if (!flowName.value.trim()) {
    message.warning('请输入流程名称');
    return;
  }
  
  const validation = validateFlow();
  if (!validation.valid) {
    message.warning(`流程配置不完整:\n${validation.errors.join('\n')}`);
    return;
  }
  
  definitionMeta.value.flowName = flowName.value;
  if (visibilityScopeText.value.trim()) {
    const parsedScope = parseVisibilityScope(visibilityScopeText.value);
    if (!parsedScope) {
      message.error('可见范围JSON格式不正确');
      return;
    }
    definitionMeta.value.visibilityScope = parsedScope;
  } else {
    definitionMeta.value.visibilityScope = undefined;
  }

  if (definitionMeta.value.isLowCodeFlow) {
    lfFormPayload.value = {
      formJson: lfFormModel.value ?? { widgetList: [] },
      formFields: lfFormPayload.value?.formFields ?? []
    };
  } else {
    lfFormPayload.value = undefined;
  }
  const definitionJson = ApprovalTreeConverter.treeToDefinitionJson(
    flowTree.value,
    definitionMeta.value,
    lfFormPayload.value
  );
  
  const visibilityScopeJson = definitionMeta.value.visibilityScope
    ? JSON.stringify(definitionMeta.value.visibilityScope)
    : undefined;

  const payload = {
    name: flowName.value,
    definitionJson,
    description: definitionMeta.value.description,
    category: definitionMeta.value.category,
    visibilityScopeJson,
    isQuickEntry: !!definitionMeta.value.isQuickEntry
  };

  try {
    if (flowId.value) {
      await updateApprovalFlow(flowId.value, payload);
      message.success('保存成功');
    } else {
      const result = await createApprovalFlow(payload);
      flowId.value = result.id;
      router.replace(`/approval/designer/${result.id}`);
      message.success('创建成功');
    }
  } catch (err) {
    message.error(err instanceof Error ? err.message : '保存失败');
  }
};

const handlePublish = async () => {
  if (!flowId.value) {
    message.warning('请先保存流程');
    return;
  }
  
  try {
    await publishApprovalFlow(flowId.value);
    message.success('发布成功');
    router.push('/approval/flows');
  } catch (err) {
    message.error(err instanceof Error ? err.message : '发布失败');
  }
};

const parseVisibilityScope = (value: string): VisibilityScope | null => {
  try {
    const parsed = JSON.parse(value) as VisibilityScope;
    if (!parsed || typeof parsed !== 'object') return null;
    if (!parsed.scopeType) return null;
    return parsed;
  } catch {
    return null;
  }
};

onMounted(() => {
  loadFlow();
});
</script>

<style scoped>
.designer-container {
  height: calc(100vh - 260px);
  min-height: 500px;
  border: 1px solid #eef0f5;
  border-radius: 8px;
  position: relative;
  overflow: hidden;
}

.designer-steps {
  margin-bottom: 16px;
}

.step-content {
  margin-bottom: 16px;
}

.basic-form {
  background: #fff;
  padding: 16px;
  border: 1px solid #f0f0f0;
  border-radius: 6px;
  max-width: 720px;
}

.step-actions {
  display: flex;
  justify-content: flex-end;
  margin-top: 12px;
}
</style>
