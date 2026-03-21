<template>
  <div
    class="dd-node dd-node--route"
    :class="{ 'is-error': data.error }"
    @click="handleClick"
  >
    <div class="dd-node__header dd-node__header--route">
      <SwapOutlined class="dd-node__icon" />
      <span class="dd-node__title">{{ data.nodeName || t('approvalDesigner.shapeRouteDefault') }}</span>
      <CloseOutlined class="dd-node__delete" @click.stop="handleDelete" />
    </div>
    <div class="dd-node__body">
      <span v-if="data.routeTargetNodeId" class="dd-node__text">{{ t('approvalDesigner.shapeJumpTo', { id: data.routeTargetNodeId }) }}</span>
      <span v-else class="dd-node__placeholder">{{ t('approvalDesigner.shapePickRouteTarget') }}</span>
      <RightOutlined class="dd-node__arrow" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { inject, ref, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { SwapOutlined, CloseOutlined, RightOutlined } from '@ant-design/icons-vue';

const { t } = useI18n();
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

const handleDelete = () => {
  const node = getNode();
  node.trigger('node:delete', { nodeId: data.value.id });
};
</script>

<style scoped>
.dd-node__header--route {
  background: #718dff;
}
</style>
