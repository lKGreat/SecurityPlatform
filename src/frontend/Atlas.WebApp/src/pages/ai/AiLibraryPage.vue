<template>
  <a-card :title="t('ai.library.pageTitle')" :bordered="false">
    <div class="toolbar">
      <a-space wrap>
        <a-input-search
          v-model:value="keyword"
          :placeholder="t('ai.library.searchPlaceholder')"
          style="width: 260px"
          @search="loadData"
        />
        <a-select
          v-model:value="resourceType"
          allow-clear
          :placeholder="t('ai.library.resourceTypePlaceholder')"
          style="width: 180px"
          :options="resourceTypeOptions"
          @change="loadData"
        />
      </a-space>
    </div>

    <a-table :columns="columns" :data-source="items" :loading="loading" row-key="key" :pagination="false">
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'type'">
          <a-tag color="blue">{{ record.resourceType }}</a-tag>
        </template>
        <template v-if="column.key === 'name'">
          <a-typography-link @click="goPath(record.path)">
            {{ record.name }}
          </a-typography-link>
        </template>
      </template>
    </a-table>

    <div class="pager">
      <a-pagination
        v-model:current="pageIndex"
        v-model:page-size="pageSize"
        :total="total"
        show-size-changer
        :page-size-options="['10', '20', '50']"
        @change="loadData"
      />
    </div>
  </a-card>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, onUnmounted } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import { useRouter } from "vue-router";
import { message } from "ant-design-vue";
import { getAiWorkspaceLibrary, type AiLibraryItem } from "@/services/api-ai-workspace";

const router = useRouter();
const loading = ref(false);
const items = ref<Array<AiLibraryItem & { key: string }>>([]);
const total = ref(0);
const pageIndex = ref(1);
const pageSize = ref(20);
const keyword = ref("");
const resourceType = ref<string | undefined>(undefined);

const columns = computed(() => [
  { title: t("ai.promptLib.colName"), dataIndex: "name", key: "name" },
  { title: t("ai.search.colType"), key: "type", width: 140 },
  { title: t("ai.promptLib.labelDescription"), dataIndex: "description", key: "description" },
  { title: t("ai.workflow.colUpdatedAt"), dataIndex: "updatedAt", key: "updatedAt", width: 180 }
]);

const resourceTypeOptions = computed(() => [
  { label: t("ai.library.typeAgent"), value: "agent" },
  { label: t("ai.library.typeKb"), value: "knowledge-base" },
  { label: t("ai.library.typeWorkflow"), value: "workflow" },
  { label: t("ai.library.typeApp"), value: "app" },
  { label: t("ai.library.typePrompt"), value: "prompt" }
]);

async function loadData() {
  loading.value = true;
  try {
    const result  = await getAiWorkspaceLibrary({
      keyword: keyword.value || undefined,
      resourceType: resourceType.value,
      pageIndex: pageIndex.value,
      pageSize: pageSize.value
    });

    if (!isMounted.value) return;
    items.value = result.items.map((item) => ({
      ...item,
      key: `${item.resourceType}-${item.resourceId}`
    }));
    total.value = Number(result.total);
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.library.loadFailed"));
  } finally {
    loading.value = false;
  }
}

async function goPath(path: string) {
  await router.push(path);

  if (!isMounted.value) return;
}

onMounted(() => {
  void loadData();
});
</script>

<style scoped>
.toolbar {
  margin-bottom: 16px;
}

.pager {
  margin-top: 16px;
  display: flex;
  justify-content: flex-end;
}
</style>
