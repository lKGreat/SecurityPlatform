<template>
  <a-tabs
    type="editable-card"
    hide-add
    size="small"
    :active-key="activeKey"
    @change="onChange"
    @edit="onEdit"
  >
    <a-tab-pane
      v-for="tag in tags"
      :key="tag.path"
      :tab="tag.title"
      :closable="tag.path !== '/'"
    />
  </a-tabs>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";

interface TagItem {
  path: string;
  title: string;
}

const route = useRoute();
const router = useRouter();
const tags = ref<TagItem[]>([{ path: "/", title: "首页" }]);

watch(
  () => route.fullPath,
  () => {
    const exists = tags.value.find((tag) => tag.path === route.fullPath);
    if (!exists && route.path !== "/login" && route.path !== "/register") {
      tags.value.push({
        path: route.fullPath,
        title: String(route.meta?.title || route.name || route.path)
      });
    }
  },
  { immediate: true }
);

const activeKey = computed(() => route.fullPath);

function onChange(key: string) {
  router.push(key);
}

function onEdit(targetKey: string | MouseEvent | KeyboardEvent, action: "remove" | "add") {
  if (action !== "remove" || typeof targetKey !== "string") return;
  const idx = tags.value.findIndex((tag) => tag.path === targetKey);
  if (idx < 0) return;
  tags.value.splice(idx, 1);
  if (route.fullPath === targetKey) {
    const next = tags.value[idx - 1] ?? tags.value[0];
    router.push(next.path);
  }
}
</script>
