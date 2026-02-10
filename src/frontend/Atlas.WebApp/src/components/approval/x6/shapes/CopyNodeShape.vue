<template>
  <div
    class="dd-node dd-node--copy"
    :class="{ 'is-error': data.error }"
    @click="handleClick"
  >
    <div class="dd-node__header dd-node__header--copy">
      <SendOutlined class="dd-node__icon" />
      <span class="dd-node__title">{{ data.nodeName || '抄送人' }}</span>
      <CloseOutlined class="dd-node__delete" @click.stop="handleDelete" />
    </div>
    <div class="dd-node__body">
      <span v-if="copyLabel" class="dd-node__text">{{ copyLabel }}</span>
      <span v-else class="dd-node__placeholder">请选择抄送人</span>
      <RightOutlined class="dd-node__arrow" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { inject, ref, computed, onMounted } from 'vue';
import { SendOutlined, CloseOutlined, RightOutlined } from '@ant-design/icons-vue';
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

const copyLabel = computed(() => {
  const users = data.value.copyToUsers as string[] | undefined;
  if (!users || users.length === 0) return '';
  return users.join(', ');
});

const handleClick = () => {
  const node = getNode();
  node.trigger('node:select', { nodeData: data.value });
};

const handleDelete = () => {
  const node = getNode();
  node.trigger('node:delete', { nodeId: data.value.id });
};
</script>
