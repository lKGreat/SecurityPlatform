<template>
  <div class="dd-node dd-node--start" @click="handleClick">
    <div class="dd-node__header dd-node__header--start">
      <span class="dd-node__title">{{ data.nodeName || '发起人' }}</span>
    </div>
    <div class="dd-node__body">
      <span class="dd-node__text">所有人</span>
      <RightOutlined class="dd-node__arrow" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { inject, ref, onMounted } from 'vue';
import { RightOutlined } from '@ant-design/icons-vue';
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

const handleClick = () => {
  const node = getNode();
  node.trigger('node:select', { nodeData: data.value });
};
</script>
