<template>
  <div class="dd-cond-header">
    <div class="dd-cond-header__label">{{ title }}</div>
    <button class="dd-cond-header__add-btn" @click.stop="handleAddBranch">
      添加条件
    </button>
  </div>
</template>

<script setup lang="ts">
import { inject, ref, computed, onMounted } from 'vue';
import type { Node } from '@antv/x6';

const getNode = inject<() => Node>('getNode')!;
const data = ref<Record<string, unknown>>({});

onMounted(() => {
  const node = getNode();
  data.value = node.getData() || {};
  node.on('change:data', ({ current }: { current: Record<string, unknown> }) => {
    data.value = { ...current };
  });
});

const title = computed(() => {
  const t = data.value.nodeType as string;
  if (t === 'dynamicCondition') return '动态条件';
  if (t === 'parallelCondition') return '条件并行';
  return '条件分支';
});

const handleAddBranch = () => {
  const node = getNode();
  node.trigger('condition:addBranch', { nodeId: data.value.id });
};
</script>
