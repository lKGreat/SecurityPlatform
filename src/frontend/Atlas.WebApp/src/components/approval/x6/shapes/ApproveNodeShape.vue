<template>
  <div
    class="dd-node dd-node--approve"
    :class="{ 'is-error': data.error }"
    @click="handleClick"
  >
    <div class="dd-node__header dd-node__header--approve">
      <UserOutlined class="dd-node__icon" />
      <span class="dd-node__title">{{ data.nodeName || t('approvalDesigner.shapeApproveDefault') }}</span>
      <CloseOutlined class="dd-node__delete" @click.stop="handleDelete" />
    </div>
    <div class="dd-node__body">
      <span v-if="assigneeLabel" class="dd-node__text">{{ assigneeLabel }}</span>
      <span v-else class="dd-node__placeholder">{{ t('approvalDesigner.shapePickApprover') }}</span>
      <RightOutlined class="dd-node__arrow" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { inject, ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue-i18n';
import { UserOutlined, CloseOutlined, RightOutlined } from '@ant-design/icons-vue';
import type { Node } from '@antv/x6';

const { t } = useI18n();

function assigneeTypeName(typeNum: number): string {
  switch (typeNum) {
    case 0:
      return t('approvalDesigner.assigneeUser');
    case 1:
      return t('approvalDesigner.assigneeRole');
    case 2:
      return t('approvalDesigner.assigneeDeptLeader');
    case 3:
      return t('approvalDesigner.assigneeOptLoop');
    case 4:
      return t('approvalDesigner.assigneeOptLevel');
    case 5:
      return t('approvalDesigner.assigneeDirectLeader');
    case 6:
      return t('approvalDesigner.assigneeInitiator');
    case 7:
      return t('approvalDesigner.assigneeHrbp');
    case 8:
      return t('approvalDesigner.assigneeInitiatorPick');
    case 9:
      return t('approvalDesigner.assigneeBizField');
    case 10:
      return t('approvalDesigner.assigneeExternal');
    default:
      return t('approvalDesigner.assigneeUser');
  }
}

const getNode = inject<() => Node>('getNode')!;
const data = ref<Record<string, unknown>>({});

onMounted(() => {
  const node = getNode();
  data.value = node.getData() || {};
  node.on('change:data', ({ current }: { current: Record<string, unknown> }) => {
    data.value = { ...current };
  });
});

const assigneeLabel = computed(() => {
  // 优先使用 Store 计算的展示标签
  if (data.value._displayLabel) {
    return data.value._displayLabel as string;
  }
  // 回退到本地计算
  const val = data.value.assigneeValue as string;
  const typeNum = (data.value.assigneeType ?? 0) as number;
  const typeName = assigneeTypeName(typeNum);
  if (!val) {
    if (typeNum === 2 || typeNum === 3 || typeNum === 5 || typeNum === 6 || typeNum === 7 || typeNum === 8) {
      return typeName;
    }
    return '';
  }
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
