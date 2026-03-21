<template>
  <div class="lf-form-designer">
    <a-alert
      type="info"
      show-icon
      :message="t('approvalDesigner.lfAlertJsonModeTitle')"
      :description="t('approvalDesigner.lfAlertJsonModeDesc')"
    />
    <div class="designer-shell">
      <div class="designer-panel">
        <div v-if="!vformReady" class="designer-loading">{{ t('approvalDesigner.lfDesignerLoading') }}</div>
        <v-form-designer v-else ref="designerRef"></v-form-designer>
      </div>
      <div class="json-panel">
        <a-form layout="vertical" class="form-container">
          <a-form-item :label="t('approvalDesigner.lfLabelFormJson')">
            <a-textarea
              v-model:value="formJsonText"
              :rows="14"
              :placeholder="t('approvalDesigner.lfPhFormJson')"
            />
          </a-form-item>
          <a-space>
            <a-button type="primary" @click="applyJson">{{ t('approvalDesigner.lfBtnImportJson') }}</a-button>
            <a-button @click="syncFromDesigner">{{ t('approvalDesigner.lfBtnSyncFields') }}</a-button>
            <a-button @click="exportJson">{{ t('approvalDesigner.lfBtnExportJson') }}</a-button>
            <a-button @click="formatJson">{{ t('approvalDesigner.lfBtnFormatJson') }}</a-button>
          </a-space>
        </a-form>
      </div>
    </div>
    <a-divider />
    <div class="field-preview">
      <div class="field-title">{{ t('approvalDesigner.lfFieldListTitle') }}</div>
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
import { getCurrentInstance, onMounted, ref, watch, onUnmounted, computed } from 'vue';
import { useI18n } from 'vue-i18n';

const { t } = useI18n();

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import type { LfFormField, FormJson, FormWidget } from '@/types/approval-definition';
import { message } from 'ant-design-vue';

const props = defineProps<{
  modelValue?: FormJson;
}>();

const emit = defineEmits<{
  'update:modelValue': [value: FormJson];
  'update:formFields': [fields: LfFormField[]];
}>();

const formJsonText = ref('');
const formFields = ref<LfFormField[]>([]);
const designerRef = ref<VFormDesignerInstance | null>(null);
const vformReady = ref(false);

const columns = computed(() => [
  { title: t('approvalDesigner.lfColFieldId'), dataIndex: 'fieldId' },
  { title: t('approvalDesigner.lfColFieldName'), dataIndex: 'fieldName' },
  { title: t('approvalDesigner.lfColFieldType'), dataIndex: 'fieldType' },
  { title: t('approvalDesigner.lfColValueType'), dataIndex: 'valueType' }
]);

watch(
  () => props.modelValue,
  (value) => {
    if (value) {
      formJsonText.value = JSON.stringify(value, null, 2);
      formFields.value = extractFields(value);
      if (designerRef.value) {
        designerRef.value.setFormJson(value);
      }
    } else {
      formJsonText.value = '';
      formFields.value = [];
    }
  },
  { immediate: true }
);

const applyJson = () => {
  if (!formJsonText.value.trim()) {
    emit('update:modelValue', { widgetList: [] });
    emit('update:formFields', []);
    formFields.value = [];
    return;
  }

  const parsed = parseFormJson(formJsonText.value);
  if (!parsed) {
    message.error(t('approvalDesigner.lfMsgJsonParseErr'));
    return;
  }

  const fields = extractFields(parsed);
  formFields.value = fields;
  emit('update:modelValue', parsed);
  emit('update:formFields', fields);
  if (designerRef.value) {
    designerRef.value.setFormJson(parsed);
  }
  message.success(t('approvalDesigner.lfMsgJsonApplied'));
};

const exportJson = () => {
  if (!designerRef.value) return;
  const formJson = designerRef.value.getFormJson();
  formJsonText.value = JSON.stringify(formJson, null, 2);
  const fields = extractFields(formJson);
  formFields.value = fields;
  emit('update:modelValue', formJson);
  emit('update:formFields', fields);
};

const syncFromDesigner = () => {
  if (!designerRef.value) return;
  const formJson = designerRef.value.getFormJson();
  const fields = extractFields(formJson);
  formFields.value = fields;
  emit('update:modelValue', formJson);
  emit('update:formFields', fields);
};

const formatJson = () => {
  if (!formJsonText.value.trim()) return;
  const parsed = parseFormJson(formJsonText.value);
  if (!parsed) {
    message.error(t('approvalDesigner.lfMsgFormatErr'));
    return;
  }
  formJsonText.value = JSON.stringify(parsed, null, 2);
};

const extractFields = (formJson: FormJson): LfFormField[] => {
  const widgetList = formJson.widgetList;
  if (!Array.isArray(widgetList)) return [];

  const fields: LfFormField[] = [];
  const traverse = (widgets: FormWidget[]) => {
    widgets.forEach((widget) => {
      if (!widget) return;
      if (Array.isArray(widget.widgetList)) {
        traverse(widget.widgetList);
        return;
      }
      const fieldId = widget.id ?? widget.options?.name;
      if (!fieldId) return;
      const fieldName = widget.options?.label ?? widget.label ?? fieldId;
      fields.push({
        fieldId,
        fieldName,
        fieldType: widget.type ?? 'unknown',
        valueType: widget.options?.fieldType ?? 'String',
        options: widget.options?.options ?? []
      });
    });
  };

  traverse(widgetList);
  return fields;
};

const parseFormJson = (value: string): FormJson | null => {
  try {
    const parsed = JSON.parse(value) as FormJson;
    if (!parsed || typeof parsed !== 'object') return null;
    if (parsed.widgetList && !Array.isArray(parsed.widgetList)) return null;
    return parsed;
  } catch {
    return null;
  }
};

onMounted(() => {
  void initVForm();
  if (props.modelValue && designerRef.value) {
    designerRef.value.setFormJson(props.modelValue);
  }
});

const initVForm = async () => {
  const instance = getCurrentInstance();
  if (!instance) return;

  const mod  = await import('vform3-builds');

  if (!isMounted.value) return;
  await import('vform3-builds/dist/designer.style.css');
  if (!isMounted.value) return;

  const app = instance.appContext.app;
  const globals = app.config.globalProperties as { __vform3_installed__?: boolean };
  if (!globals.__vform3_installed__) {
    app.use(mod.default);
    globals.__vform3_installed__ = true;
  }
  vformReady.value = true;
};

type VFormDesignerInstance = {
  getFormJson: () => FormJson;
  setFormJson: (formJson: FormJson) => void;
};
</script>

<style scoped>
.lf-form-designer {
  background: var(--color-bg-container);
  padding: 16px;
  border: 1px solid var(--color-bg-hover);
  border-radius: 6px;
}

.designer-shell {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: 12px;
  margin-top: 12px;
}

.designer-panel {
  border: 1px solid var(--color-bg-hover);
  border-radius: 6px;
  min-height: 520px;
  overflow: hidden;
}
.designer-loading {
  padding: 16px;
  color: var(--color-text-tertiary);
}

.json-panel {
  border: 1px solid var(--color-bg-hover);
  border-radius: 6px;
  padding: 12px;
}

.field-preview {
  margin-top: 12px;
}

.field-title {
  font-weight: 600;
  margin-bottom: 8px;
}
</style>
