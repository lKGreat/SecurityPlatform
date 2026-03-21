<template>
  <a-form layout="vertical">
    <a-form-item :label="t('ai.workflowNode.labelModel')">
      <a-input v-model:value="local.model" :placeholder="t('ai.workflowNode.modelPlaceholder')" />
    </a-form-item>
    <a-form-item :label="t('ai.workflowNode.labelPrompt')">
      <a-textarea v-model:value="local.promptTemplate" :rows="4" :placeholder="t('ai.workflowNode.promptPlaceholder')" />
    </a-form-item>
    <a-form-item :label="t('ai.workflowNode.labelTemperature')">
      <a-input-number v-model:value="local.temperature" :min="0" :max="2" :step="0.1" />
    </a-form-item>
  </a-form>
</template>

<script setup lang="ts">
import { reactive, watch } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();

const props = defineProps<{
  modelValue: Record<string, unknown>;
}>();

const emit = defineEmits<{
  (e: "update:modelValue", value: Record<string, unknown>): void;
}>();

const local = reactive({
  model: "",
  promptTemplate: "",
  temperature: 0.7
});

watch(
  () => props.modelValue,
  (v) => {
    Object.assign(local, {
      model: (v?.["model"] as string) ?? "",
      promptTemplate: (v?.["promptTemplate"] as string) ?? "",
      temperature: Number(v?.["temperature"] ?? 0.7)
    });
  },
  { immediate: true, deep: true }
);

watch(
  local,
  () => {
    emit("update:modelValue", {
      ...props.modelValue,
      model: local.model,
      promptTemplate: local.promptTemplate,
      temperature: local.temperature
    });
  },
  { deep: true }
);
</script>
