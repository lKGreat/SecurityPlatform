<template>
  <div class="designer-panel">
    <a-tabs v-model:active-key="activeKey">
      <a-tab-pane key="basic" tab="基础信息">
        <slot name="basic" />
      </a-tab-pane>
      <a-tab-pane key="node" tab="节点属性">
        <slot name="node" />
      </a-tab-pane>
      <a-tab-pane key="validate" tab="校验结果">
        <slot name="validate" />
      </a-tab-pane>
    </a-tabs>
  </div>
</template>

<script setup lang="ts">
import { ref } from "vue";

const activeKey = ref("basic");
</script>

<style scoped>
.designer-panel {
  border: 1px solid #f0f0f0;
  border-radius: 6px;
  background: #fff;
  padding: 12px;
  min-height: 480px;
}
</style>
