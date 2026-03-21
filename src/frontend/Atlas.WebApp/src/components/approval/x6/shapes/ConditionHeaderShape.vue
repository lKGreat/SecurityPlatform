<template>
  <div class="dd-cond-header">
    <div class="dd-cond-header__label">{{ title }}</div>
    <button class="dd-cond-header__add-btn" @click.stop="handleAddBranch">
      {{ t('approvalDesigner.condHeaderAddCondition') }}
    </button>
  </div>
</template>

<script setup lang="ts">
import { inject, ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import type { Node } from '@antv/x6';

const { t } = useI18n();

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
  const nt = data.value.nodeType as string;
  if (nt === 'dynamicCondition') return t('approvalDesigner.nodeTypeLabelDynamicCondition');
  if (nt === 'parallelCondition') return t('approvalDesigner.nodeTypeLabelParallelCondition');
  return t('approvalDesigner.palNodeCondition');
});

const handleAddBranch = () => {
  const node = getNode();
  node.trigger('condition:addBranch', { nodeId: data.value.id });
};
</script>
