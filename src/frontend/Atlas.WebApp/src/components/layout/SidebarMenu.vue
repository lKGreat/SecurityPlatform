<template>
  <a-menu
    theme="dark"
    mode="inline"
    :selected-keys="selectedKeys"
    :open-keys="openKeys"
    @openChange="onOpenChange"
  >
    <template v-for="item in menuTree" :key="item.name + item.path">
      <a-sub-menu v-if="item.children && item.children.length > 0" :key="item.path">
        <template #title>{{ item.meta?.title || item.name }}</template>
        <a-menu-item
          v-for="child in item.children"
          :key="child.path"
          @click="go(child.path)"
        >
          {{ child.meta?.title || child.name }}
        </a-menu-item>
      </a-sub-menu>
      <a-menu-item v-else :key="item.path" @click="go(item.path)">
        {{ item.meta?.title || item.name }}
      </a-menu-item>
    </template>
  </a-menu>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { usePermissionStore } from "@/stores/permission";

const permissionStore = usePermissionStore();
const router = useRouter();
const route = useRoute();
const openKeys = ref<string[]>([]);

const menuTree = computed(() =>
  permissionStore.sidebarRouters.filter((item) => !(item.hidden ?? false))
);

const selectedKeys = computed(() => [route.path]);

watch(
  () => route.path,
  () => {
    const first = route.path.split("/").filter(Boolean)[0];
    openKeys.value = first ? [`/${first}`] : [];
  },
  { immediate: true }
);

function onOpenChange(keys: string[]) {
  openKeys.value = keys;
}

function go(path: string) {
  if (!path) return;
  router.push(path);
}
</script>
