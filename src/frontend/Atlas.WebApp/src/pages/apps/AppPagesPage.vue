<template>
  <div class="app-pages-page">
    <a-page-header
      :title="`${t('appsPages.titlePrefix')} - ${appDetail?.name ?? ''}`"
      :sub-title="appDetail?.appKey ?? ''"
    >
      <template #extra>
        <a-button @click="go(`/apps/${appId}/dashboard`)">{{ t("appsPages.backDashboard") }}</a-button>
        <a-button type="primary" @click="go(`/apps/${appId}/builder`)">{{ t("appsPages.openDesigner") }}</a-button>
      </template>
    </a-page-header>

    <a-card style="margin-top: 12px">
      <a-table
        :columns="columns"
        :data-source="pages"
        :loading="loading"
        row-key="id"
        :pagination="false"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'isPublished'">
            <a-tag :color="record.isPublished ? 'green' : 'default'">
              {{ record.isPublished ? t("appsPages.published") : t("appsPages.draft") }}
            </a-tag>
          </template>
          <template v-else-if="column.key === 'actions'">
            <a-space>
              <a-button type="link" @click="go(`/apps/${appId}/builder`)">{{ t("appsPages.design") }}</a-button>
              <a-button
                v-if="record.pageKey"
                type="link"
                @click="go(`/apps/${appId}/run/${record.pageKey}`)"
              >
                {{ t("appsPages.runtimePreview") }}
              </a-button>
              <a-button
                v-if="record.pageKey && appDetail?.appKey"
                type="link"
                @click="go(`/r/${appDetail.appKey}/${record.pageKey}`)"
              >
                {{ t("appsPages.goProduction") }}
              </a-button>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch, onUnmounted } from "vue";
import { useI18n } from "vue-i18n";

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import { useRoute, useRouter } from "vue-router";
import { message } from "ant-design-vue";
import { getLowCodeAppDetail } from "@/services/lowcode";
import type { LowCodeAppDetail, LowCodePageListItem } from "@/types/lowcode";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();

const appId = computed(() => String(route.params.appId ?? ""));
const appDetail = ref<LowCodeAppDetail | null>(null);
const pages = ref<LowCodePageListItem[]>([]);
const loading = ref(false);

const columns = computed(() => [
  { title: t("appsPages.colName"), dataIndex: "name", key: "name", ellipsis: true },
  { title: t("appsPages.colPageKey"), dataIndex: "pageKey", key: "pageKey", width: 220 },
  { title: t("appsPages.colRoute"), dataIndex: "routePath", key: "routePath", width: 240 },
  { title: t("appsPages.colVersion"), dataIndex: "version", key: "version", width: 100 },
  { title: t("appsPages.colPublishStatus"), key: "isPublished", width: 120 },
  { title: t("appsPages.colActions"), key: "actions", width: 180 }
]);

async function loadPages() {
  if (!appId.value) {
    appDetail.value = null;
    pages.value = [];
    return;
  }

  loading.value = true;
  try {
    const detail  = await getLowCodeAppDetail(appId.value);

    if (!isMounted.value) return;
    appDetail.value = detail;
    pages.value = [...detail.pages].sort((a, b) => a.sortOrder - b.sortOrder);
  } catch (error) {
    message.error((error as Error).message || t("appsPages.loadFailed"));
  } finally {
    loading.value = false;
  }
}

function go(path: string) {
  router.push(path);
}

onMounted(loadPages);
watch(appId, () => {
  loadPages();
});
</script>

<style scoped>
.app-pages-page {
  padding: 8px;
}
</style>
