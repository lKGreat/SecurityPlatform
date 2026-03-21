<template>
  <a-space direction="vertical" style="width: 100%" :size="16">
    <a-card :title="t('ai.workspace.pageTitle')" :bordered="false">
      <WorkspaceSwitcher
        :name="workspace.name"
        :theme="workspace.theme"
        @save="handleWorkspaceSave"
      />
      <a-divider />
      <a-descriptions :column="2" size="small" bordered>
        <a-descriptions-item :label="t('ai.workspace.lastVisited')">{{ workspace.lastVisitedPath }}</a-descriptions-item>
        <a-descriptions-item :label="t('ai.workspace.favoriteCount')">{{ workspace.favoriteResourceIds.length }}</a-descriptions-item>
      </a-descriptions>
    </a-card>

    <a-card :title="t('ai.workspace.frequentResources')" :bordered="false">
      <a-list :data-source="libraryItems" :loading="loading">
        <template #renderItem="{ item }">
          <a-list-item>
            <a-space>
              <a-tag color="blue">{{ item.resourceType }}</a-tag>
              <a-typography-link @click="goPath(item.path)">{{ item.name }}</a-typography-link>
              <span class="description">{{ item.description || "-" }}</span>
            </a-space>
          </a-list-item>
        </template>
      </a-list>
    </a-card>
  </a-space>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref, onUnmounted } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import { useRouter } from "vue-router";
import { message } from "ant-design-vue";
import WorkspaceSwitcher from "@/components/ai/WorkspaceSwitcher.vue";
import {
  getAiWorkspaceLibrary,
  getCurrentAiWorkspace,
  updateCurrentAiWorkspace,
  type AiLibraryItem
} from "@/services/api-ai-workspace";

const router = useRouter();
const loading = ref(false);
const workspace = reactive({
  name: "",
  theme: "light",
  lastVisitedPath: "/ai/workspace",
  favoriteResourceIds: [] as number[]
});
const libraryItems = ref<AiLibraryItem[]>([]);

async function loadWorkspace() {
  const data  = await getCurrentAiWorkspace();

  if (!isMounted.value) return;
  Object.assign(workspace, data);
  if (!workspace.name) {
    workspace.name = t("ai.workspace.defaultName");
  }
}

async function loadLibrary() {
  loading.value = true;
  try {
    const result  = await getAiWorkspaceLibrary({
      pageIndex: 1,
      pageSize: 10
    });

    if (!isMounted.value) return;
    libraryItems.value = result.items;
  } finally {
    loading.value = false;
  }
}

async function handleWorkspaceSave(payload: { name: string; theme: string }) {
  try {
    const updated  = await updateCurrentAiWorkspace({
      name: payload.name,
      theme: payload.theme,
      lastVisitedPath: workspace.lastVisitedPath,
      favoriteResourceIds: workspace.favoriteResourceIds
    });

    if (!isMounted.value) return;
    Object.assign(workspace, updated);
    message.success(t("ai.workspace.saveSuccess"));
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.workspace.saveFailed"));
  }
}

async function goPath(path: string) {
  workspace.lastVisitedPath = path;
  await router.push(path);

  if (!isMounted.value) return;
}

onMounted(async () => {
  await loadWorkspace();

  if (!isMounted.value) return;
  await loadLibrary();

  if (!isMounted.value) return;
});
</script>

<style scoped>
.description {
  color: #999;
  font-size: 12px;
}
</style>
