<template>
  <div class="palette-panel">
    <div class="title">节点库</div>
    <a-space wrap>
      <a-tag
        v-for="item in items"
        :key="item.type"
        color="blue"
        @click="() => emit('add', item.type)"
      >
        {{ item.label }}
      </a-tag>
    </a-space>
  </div>
</template>

<script setup lang="ts">
import { paletteItems } from "./NodePalette";
import type { NodeType } from "@/types/workflow";

const items = paletteItems;
const emit = defineEmits<{
  (e: "add", type: NodeType): void;
}>();
</script>

<style scoped>
.palette-panel {
  border: 1px solid #f0f0f0;
  border-radius: 6px;
  padding: 12px;
  background: #fff;
}
.title {
  font-weight: 600;
  margin-bottom: 8px;
}
</style>
