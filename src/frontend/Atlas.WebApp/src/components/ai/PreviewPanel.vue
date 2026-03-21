<template>
  <a-card size="small" :title="t('ai.previewPanel.title')">
    <a-alert :message="title" type="info" show-icon />
    <pre class="preview-content">{{ content }}</pre>
  </a-card>
</template>

<script setup lang="ts">
import { useI18n } from "vue-i18n";

const { t } = useI18n();

defineProps<{
  title: string;
  content: string;
}>();
</script>

<style scoped>
.preview-content {
  margin-top: 10px;
  padding: 10px;
  background: #fafafa;
  border-radius: 8px;
  max-height: 220px;
  overflow: auto;
  white-space: pre-wrap;
  word-break: break-all;
}
</style>
