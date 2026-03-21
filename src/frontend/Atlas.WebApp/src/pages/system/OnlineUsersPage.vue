<template>
  <a-card :title="t('systemOnlineUsers.cardTitle')" :bordered="false">
    <div class="crud-toolbar">
      <a-space wrap>
        <a-input-search
          v-model:value="keyword"
          :placeholder="t('systemOnlineUsers.searchPlaceholder')"
          allow-clear
          style="width: 260px"
          @search="handleSearch"
        />
        <a-button @click="handleSearch">{{ t("commonUi.refresh") }}</a-button>
      </a-space>
    </div>

    <a-table
      :columns="columns"
      :data-source="dataList"
      :loading="loading"
      :pagination="pagination"
      row-key="sessionId"
      :locale="{ emptyText: t('systemOnlineUsers.emptyText') }"
      @change="onTableChange"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'loginTime'">
          {{ formatTime(record.loginTime) }}
        </template>
        <template v-else-if="column.key === 'lastSeenAt'">
          {{ formatTime(record.lastSeenAt) }}
        </template>
        <template v-else-if="column.key === 'expiresAt'">
          {{ formatTime(record.expiresAt) }}
        </template>
        <template v-else-if="column.key === 'actions'">
          <a-popconfirm
            :title="t('systemOnlineUsers.forceLogoutConfirm')"
            :ok-text="t('systemOnlineUsers.forceLogoutOk')"
            ok-type="danger"
            :cancel-text="t('common.cancel')"
            @confirm="handleForceLogout(record.sessionId)"
          >
            <a-button type="link" danger size="small">{{ t("systemOnlineUsers.forceLogoutBtn") }}</a-button>
          </a-popconfirm>
        </template>
      </template>
    </a-table>
  </a-card>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted, onUnmounted, computed } from "vue";
import { useI18n } from "vue-i18n";

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import { message } from "ant-design-vue";
import type { TablePaginationConfig } from "ant-design-vue";
import { getOnlineUsers, forceLogout, type OnlineUserDto } from "@/services/sessions";

const { t, locale } = useI18n();

const keyword = ref("");
const dataList = ref<OnlineUserDto[]>([]);
const loading = ref(false);
const pagination = reactive<TablePaginationConfig>({
  current: 1,
  pageSize: 20,
  total: 0,
  showSizeChanger: false
});

const columns = computed(() => [
  { title: t("systemOnlineUsers.colUsername"), dataIndex: "username", key: "username" },
  { title: t("systemOnlineUsers.colIp"), dataIndex: "ipAddress", key: "ipAddress" },
  { title: t("systemOnlineUsers.colClientType"), dataIndex: "clientType", key: "clientType" },
  { title: t("systemOnlineUsers.colLoginTime"), key: "loginTime", width: 180 },
  { title: t("systemOnlineUsers.colLastSeen"), key: "lastSeenAt", width: 180 },
  { title: t("systemOnlineUsers.colExpiresAt"), key: "expiresAt", width: 180 },
  { title: t("systemOnlineUsers.colActions"), key: "actions", width: 100, fixed: "right" as const }
]);

function formatTime(val: string) {
  if (!val) return "-";
  const loc = locale.value === "en-US" ? "en-US" : "zh-CN";
  return new Date(val).toLocaleString(loc, { hour12: false });
}

async function loadData() {
  loading.value = true;
  try {
    const result  = await getOnlineUsers({
      pageIndex: pagination.current ?? 1,
      pageSize: pagination.pageSize ?? 20,
      keyword: keyword.value || undefined
    });

    if (!isMounted.value) return;
    dataList.value = result.items as OnlineUserDto[];
    pagination.total = Number(result.total);
  } catch (e: unknown) {
    message.error((e instanceof Error ? e.message : undefined) || t("systemOnlineUsers.loadFailed"));
  } finally {
    loading.value = false;
  }
}

function handleSearch() {
  pagination.current = 1;
  loadData();
}

function onTableChange(pag: TablePaginationConfig) {
  pagination.current = pag.current ?? 1;
  loadData();
}

async function handleForceLogout(sessionId: string) {
  try {
    await forceLogout(sessionId);

    if (!isMounted.value) return;
    message.success(t("systemOnlineUsers.forceLogoutSuccess"));
    loadData();
  } catch (e: unknown) {
    message.error((e instanceof Error ? e.message : undefined) || t("systemOnlineUsers.operationFailed"));
  }
}

onMounted(() => {
  loadData();
});
</script>

<style scoped>
.crud-toolbar {
  margin-bottom: 16px;
}
</style>
