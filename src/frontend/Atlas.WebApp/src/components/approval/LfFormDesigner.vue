<template>
  <div class="lf-form-designer">
    <a-alert
      type="info"
      show-icon
      message="LF 低代码表单设计（JSON 模式）"
      description="当前使用 JSON 编辑模式，后续可替换为可视化表单设计器组件。"
    />
    <a-form layout="vertical" class="form-container">
      <a-form-item label="表单 JSON">
        <a-textarea
          v-model:value="formJsonText"
          :rows="16"
          placeholder="请输入表单 JSON"
        />
      </a-form-item>
      <a-space>
        <a-button type="primary" @click="applyJson">应用 JSON</a-button>
        <a-button @click="formatJson">格式化</a-button>
      </a-space>
    </a-form>
    <a-divider />
    <div class="field-preview">
      <div class="field-title">字段列表（用于条件与权限配置）</div>
      <a-table
        :columns="columns"
        :data-source="formFields"
        :pagination="false"
        row-key="fieldId"
        size="small"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import type { LfFormField } from '@/types/approval-definition';
import { message } from 'ant-design-vue';

const props = defineProps<{
  modelValue?: unknown;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: unknown];
  'update:formFields': [fields: LfFormField[]];
}>();

const formJsonText = ref('');
const formFields = ref<LfFormField[]>([]);

const columns = [
  { title: '字段ID', dataIndex: 'fieldId' },
  { title: '字段名称', dataIndex: 'fieldName' },
  { title: '字段类型', dataIndex: 'fieldType' },
  { title: '值类型', dataIndex: 'valueType' }
];

watch(
  () => props.modelValue,
  (value) => {
    if (value) {
      try {
        formJsonText.value = JSON.stringify(value, null, 2);
        formFields.value = extractFields(value);
      } catch {
        formJsonText.value = '';
        formFields.value = [];
      }
    }
  },
  { immediate: true }
);

const applyJson = () => {
  if (!formJsonText.value.trim()) {
    emit('update:modelValue', undefined);
    emit('update:formFields', []);
    formFields.value = [];
    return;
  }

  try {
    const parsed = JSON.parse(formJsonText.value);
    const fields = extractFields(parsed);
    formFields.value = fields;
    emit('update:modelValue', parsed);
    emit('update:formFields', fields);
    message.success('表单 JSON 已应用');
  } catch (err) {
    message.error('表单 JSON 解析失败，请检查格式');
  }
};

const formatJson = () => {
  if (!formJsonText.value.trim()) return;
  try {
    const parsed = JSON.parse(formJsonText.value);
    formJsonText.value = JSON.stringify(parsed, null, 2);
  } catch {
    message.error('格式化失败，请检查 JSON');
  }
};

const extractFields = (formJson: unknown): LfFormField[] => {
  if (!formJson || typeof formJson !== 'object') return [];
  const widgetList = (formJson as any).widgetList;
  if (!Array.isArray(widgetList)) return [];

  const fields: LfFormField[] = [];
  const traverse = (widgets: any[]) => {
    widgets.forEach((widget) => {
      if (!widget) return;
      if (Array.isArray(widget.widgetList)) {
        traverse(widget.widgetList);
        return;
      }
      const fieldId = widget.id || widget.options?.name;
      const fieldName = widget.options?.label || widget.label || fieldId || '未命名字段';
      if (fieldId) {
        fields.push({
          fieldId,
          fieldName,
          fieldType: widget.type || 'unknown',
          valueType: widget.options?.fieldType || 'String',
          options: widget.options?.options || []
        });
      }
    });
  };

  traverse(widgetList);
  return fields;
};
</script>

<style scoped>
.lf-form-designer {
  background: #fff;
  padding: 16px;
  border: 1px solid #f0f0f0;
  border-radius: 6px;
}

.form-container {
  margin-top: 12px;
}

.field-preview {
  margin-top: 12px;
}

.field-title {
  font-weight: 600;
  margin-bottom: 8px;
}
</style>
