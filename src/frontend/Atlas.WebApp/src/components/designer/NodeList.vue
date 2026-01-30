<template>
  <div class="node-list">
    <div class="header">
      <span>节点结构</span>
      <a-button type="link" size="small" @click="onAddStart">添加开始/结束</a-button>
    </div>
    <a-tree
      block-node
      :tree-data="treeData"
      :selectedKeys="selectedKeys"
      @select="onSelect"
    />
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue";
import type { FlowNode } from "@/types/workflow";
import { createNode } from "./NodePalette";

interface Props {
  nodes: FlowNode[];
  selectedId?: string;
}

const props = defineProps<Props>();
const emit = defineEmits<{
  (e: "select", id: string | undefined): void;
  (e: "add-default"): void;
}>();

const treeData = computed(() => nodesToTree(props.nodes));
const selectedKeys = computed(() => (props.selectedId ? [props.selectedId] : []));

const onSelect = (keys: (string | number)[]) => {
  emit("select", (keys[0] as string) || undefined);
};

const onAddStart = () => {
  if (props.nodes.length === 0) {
    emit("add-default");
  }
};

function nodesToTree(nodes: FlowNode[]): any[] {
  return nodes.map((n) => ({
    key: n.id,
    title: `${n.name} (${n.type})`,
    children: n.children ? nodesToTree(n.children) : []
  }));
}
</script>

<style scoped>
.node-list {
  border: 1px solid #f0f0f0;
  border-radius: 6px;
  padding: 12px;
  background: #fff;
}
.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}
</style>
