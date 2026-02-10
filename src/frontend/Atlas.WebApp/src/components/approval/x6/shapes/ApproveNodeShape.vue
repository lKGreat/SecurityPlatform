<template>
  <div
    class="dd-node dd-node--approve"
    :class="{ 'is-error': data.error }"
    @click="handleClick"
  >
    <div class="dd-node__header dd-node__header--approve">
      <UserOutlined class="dd-node__icon" />
      <span class="dd-node__title">{{ data.nodeName || '审批人' }}</span>
      <CloseOutlined class="dd-node__delete" @click.stop="handleDelete" />
    </div>
    <div class="dd-node__body">
      <span v-if="assigneeLabel" class="dd-node__text">{{ assigneeLabel }}</span>
      <span v-else class="dd-node__placeholder">请选择审批人</span>
      <RightOutlined class="dd-node__arrow" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { inject, ref, computed, onMounted } from 'vue';
import { UserOutlined, CloseOutlined, RightOutlined } from '@ant-design/icons-vue';
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

const ASSIGNEE_TYPE_MAP: Record<number, string> = {
  0: '指定人员',
  1: '指定角色',
  2: '部门负责人',
  3: 'HRBP',
  4: '直属领导',
  5: '层级领导',
  6: '发起人',
  7: '发起人自选',
};

const assigneeLabel = computed(() => {
  const val = data.value.assigneeValue as string;
  if (!val) return '';
  const typeNum = (data.value.assigneeType ?? 0) as number;
  const typeName = ASSIGNEE_TYPE_MAP[typeNum] || '指定人员';
  return `${typeName}: ${val}`;
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
