<template>
  <a-card size="small" :title="t('ai.traceViewer.title')">
    <a-timeline>
      <a-timeline-item v-for="(item, idx) in traces" :key="`${idx}-${item.step}`" :color="item.success ? 'green' : 'red'">
        <div class="trace-step">{{ item.step }}</div>
        <div class="trace-duration">{{ t("ai.traceViewer.duration", { ms: item.durationMs }) }}</div>
        <pre class="trace-output">{{ item.output }}</pre>
      </a-timeline-item>
    </a-timeline>
  </a-card>
</template>

<script setup lang="ts">
import { useI18n } from "vue-i18n";

const { t } = useI18n();

export interface TraceItem {
  step: string;
  durationMs: number;
  output: string;
  success: boolean;
}

defineProps<{
  traces: TraceItem[];
}>();
</script>

<style scoped>
.trace-step {
  font-weight: 600;
}

.trace-duration {
  color: #888;
  font-size: 12px;
}

.trace-output {
  margin-top: 6px;
  padding: 8px;
  border-radius: 6px;
  background: #fafafa;
  max-height: 180px;
  overflow: auto;
}
</style>
