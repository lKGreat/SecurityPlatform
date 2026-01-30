<template>
  <div class="designer-toolbar">
    <a-space>
      <a-button :loading="loading" type="primary" @click="emit('save')">保存</a-button>
      <a-button :loading="loading" @click="emit('validate')">校验</a-button>
      <a-button :loading="loading" type="default" @click="emit('preview')">预览</a-button>
      <a-button :loading="loading" type="dashed" @click="emit('publish')">发布</a-button>
    </a-space>
    <slot />
  </div>
  </template>

  <script setup lang="ts">
interface Props {
  loading?: boolean;
}
const props = defineProps<Props>();
const emit = defineEmits(["save", "validate", "preview", "publish"]);
</script>

<style scoped>
.designer-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 0;
}
</style>
