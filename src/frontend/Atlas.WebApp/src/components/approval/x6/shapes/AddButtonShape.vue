<template>
  <div class="dd-add-btn-wrap">
    <a-popover
      placement="rightTop"
      trigger="click"
      v-model:open="visible"
      :get-popup-container="getContainer"
    >
      <template #content>
        <div class="dd-add-popover">
          <div class="dd-add-popover__item" @click="handleSelect('approve')">
            <div class="dd-add-popover__icon dd-add-popover__icon--approve">
              <UserOutlined />
            </div>
            <span>审批人</span>
          </div>
          <div class="dd-add-popover__item" @click="handleSelect('copy')">
            <div class="dd-add-popover__icon dd-add-popover__icon--copy">
              <SendOutlined />
            </div>
            <span>抄送人</span>
          </div>
          <div class="dd-add-popover__item" @click="handleSelect('condition')">
            <div class="dd-add-popover__icon dd-add-popover__icon--condition">
              <BranchesOutlined />
            </div>
            <span>条件分支</span>
          </div>
          <div class="dd-add-popover__item" @click="handleSelect('parallel')">
            <div class="dd-add-popover__icon dd-add-popover__icon--approve">
              <ApartmentOutlined />
            </div>
            <span>并行审批</span>
          </div>
        </div>
      </template>
      <button class="dd-add-btn" @click.stop>
        <PlusOutlined />
      </button>
    </a-popover>
  </div>
</template>

<script setup lang="ts">
import { inject, ref, onMounted } from 'vue';
import {
  PlusOutlined,
  UserOutlined,
  SendOutlined,
  BranchesOutlined,
  ApartmentOutlined,
} from '@ant-design/icons-vue';
import type { Node } from '@antv/x6';

const getNode = inject<() => Node>('getNode')!;
const data = ref<Record<string, unknown>>({});
const visible = ref(false);

onMounted(() => {
  const node = getNode();
  data.value = node.getData() || {};
});

const handleSelect = (nodeType: string) => {
  visible.value = false;
  const node = getNode();
  node.trigger('addNode:select', {
    parentId: data.value.parentId,
    nodeType,
  });
};

const getContainer = () => {
  // Mount popover inside the X6 canvas wrapper so it scrolls with graph
  return document.querySelector('.dd-designer-canvas') || document.body;
};
</script>
