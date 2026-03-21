<template>
  <a-space>
    <a-input v-model:value="localName" :placeholder="t('ai.workspace.namePlaceholder')" style="width: 220px" />
    <a-select v-model:value="localTheme" style="width: 140px" :options="themeOptions" />
    <a-button type="primary" size="small" :loading="saving" @click="submit">{{ t("common.save") }}</a-button>
  </a-space>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();

const props = defineProps<{
  name: string;
  theme: string;
}>();

const emit = defineEmits<{
  (event: "save", payload: { name: string; theme: string }): void;
}>();

const localName = ref(props.name);
const localTheme = ref(props.theme);
const saving = ref(false);

const themeOptions = computed(() => [
  { label: t("ai.workspace.themeLight"), value: "light" },
  { label: t("ai.workspace.themeDark"), value: "dark" }
]);

watch(
  () => props.name,
  (value) => {
    localName.value = value;
  }
);

watch(
  () => props.theme,
  (value) => {
    localTheme.value = value;
  }
);

function submit() {
  saving.value = true;
  emit("save", {
    name: localName.value.trim() || t("ai.workspace.defaultName"),
    theme: localTheme.value || "light"
  });
  window.setTimeout(() => {
    saving.value = false;
  }, 200);
}
</script>
