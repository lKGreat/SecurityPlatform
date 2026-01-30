<template>
  <div class="designer-canvas">
    <slot>
      <a-empty description="画布待实现" />
    </slot>
  </div>
</template>

<script setup lang="ts">
// 画布容器，后续可扩展为可拖拽/连线组件
</script>

<style scoped>
.designer-canvas {
  border: 1px dashed #d9d9d9;
  border-radius: 6px;
  padding: 12px;
  background: #fff;
  min-height: 480px;
}
</style>
