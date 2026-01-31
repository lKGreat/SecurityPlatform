<template>
  <div ref="containerRef" class="amis-container"></div>
</template>

<script setup lang="ts">
import { onMounted, onBeforeUnmount, ref, watch } from "vue";
import { render as renderAmis } from "amis";
import type { AmisSchema } from "@/types/amis";
import type { JsonValue } from "@/types/api";
import { createAmisEnv } from "@/amis/amis-env";

interface Props {
  schema: AmisSchema;
  data?: Record<string, JsonValue>;
}

const props = defineProps<Props>();
const containerRef = ref<HTMLElement | null>(null);
const amisEnv = createAmisEnv();
const emptyData: Record<string, JsonValue> = {};

const renderSchema = () => {
  const container = containerRef.value;
  if (!container) return;
  container.innerHTML = "";
  renderAmis(props.schema, { data: props.data ?? emptyData, env: amisEnv }, container);
};

onMounted(() => {
  renderSchema();
});

watch(
  () => props.schema,
  () => {
    renderSchema();
  },
  { deep: true }
);

onBeforeUnmount(() => {
  if (containerRef.value) {
    containerRef.value.innerHTML = "";
  }
});
</script>

<style scoped>
.amis-container {
  min-height: 200px;
}
</style>
