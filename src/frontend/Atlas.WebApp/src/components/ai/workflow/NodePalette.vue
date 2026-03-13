<template>
  <div class="node-palette">
    <h4>节点面板</h4>
    <div v-for="group in groupedNodeTypes" :key="group.category" class="palette-group">
      <div class="group-title">{{ group.category }}</div>
      <a-space direction="vertical" style="width: 100%">
        <a-button
          v-for="item in group.items"
          :key="item.key"
          block
          @click="$emit('add-node', item)"
        >
          {{ item.name }}
        </a-button>
      </a-space>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue";
import type { AiWorkflowNodeTypeDto } from "@/services/api-ai-workflow";

const props = defineProps<{ nodeTypes: AiWorkflowNodeTypeDto[] }>();

defineEmits<{
  (e: "add-node", nodeType: AiWorkflowNodeTypeDto): void;
}>();

const groupedNodeTypes = computed(() => {
  const groups = new Map<string, AiWorkflowNodeTypeDto[]>();
  for (const nodeType of props.nodeTypes) {
    const category = nodeType.category || "Other";
    const list = groups.get(category) ?? [];
    list.push(nodeType);
    groups.set(category, list);
  }

  return Array.from(groups.entries()).map(([category, items]) => ({ category, items }));
});
</script>

<style scoped>
.node-palette {
  width: 220px;
  border-right: 1px solid #f0f0f0;
  padding: 12px;
  overflow-y: auto;
}

.palette-group + .palette-group {
  margin-top: 12px;
}

.group-title {
  margin-bottom: 8px;
  color: rgba(0, 0, 0, 0.65);
  font-size: 12px;
  font-weight: 600;
  text-transform: uppercase;
}
</style>
