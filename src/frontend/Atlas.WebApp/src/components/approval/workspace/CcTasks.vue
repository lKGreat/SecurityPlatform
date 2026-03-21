<template>
  <a-card :title="t('approvalWorkspace.ccCardTitle')" class="page-card">
    <FilterToolbar
      :show-refresh="true"
      @refresh="fetchData"
    >
      <a-select v-model:value="readFilter" style="width: 150px" :options="readFilterOptions" />
    </FilterToolbar>

    <a-table
      :columns="tableColumns"
      :data-source="dataSource"
      :pagination="{ ...pagination, showTotal: (total: number) => t('crud.totalItems', { total }) }"
      :loading="loading"
      row-key="id"
      @change="onTableChange"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'isRead'">
          <a-tag :color="record.isRead ? 'green' : 'orange'">
            {{ record.isRead ? t('approvalWorkspace.ccRead') : t('approvalWorkspace.ccUnread') }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'action'">
          <a-space>
            <a-button type="link" size="small" @click="viewInstance(record.instanceId)">{{ t('approvalWorkspace.ccViewFlow') }}</a-button>
            <a-button
              v-if="!record.isRead"
              type="link"
              size="small"
              @click="markRead(record.id)"
            >
              {{ t('approvalWorkspace.ccMarkRead') }}
            </a-button>
          </a-space>
        </template>
      </template>
    </a-table>
  </a-card>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch, onUnmounted } from 'vue';
import { useI18n } from 'vue-i18n';

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import { useRouter } from 'vue-router';
import { message } from 'ant-design-vue';
import type { TablePaginationConfig } from 'ant-design-vue';
import type { ApprovalCopyRecordResponse } from '@/types/api';
import { getMyCopyRecordsPaged, markCopyRecordAsRead } from '@/services/api';
import FilterToolbar from '@/components/common/FilterToolbar.vue';

const { t } = useI18n();
const router = useRouter();
const tableColumns = computed(() => [
  { title: t('approvalWorkspace.ccColInstanceId'), dataIndex: 'instanceId', key: 'instanceId' },
  { title: t('approvalWorkspace.ccColNodeId'), dataIndex: 'nodeId', key: 'nodeId' },
  { title: t('approvalWorkspace.ccColStatus'), key: 'isRead' },
  { title: t('approvalWorkspace.ccColTime'), dataIndex: 'createdAt', key: 'createdAt' },
  { title: t('approvalWorkspace.ccColActions'), key: 'action', width: 220 },
]);

const dataSource = ref<ApprovalCopyRecordResponse[]>([]);
const loading = ref(false);
const readFilter = ref<'all' | 'read' | 'unread'>('all');
const readFilterOptions = computed(() => [
  { label: t('approvalWorkspace.statusAll'), value: 'all' },
  { label: t('approvalWorkspace.ccRead'), value: 'read' },
  { label: t('approvalWorkspace.ccUnread'), value: 'unread' },
]);
const pagination = reactive<TablePaginationConfig>({
  current: 1,
  pageSize: 10,
  total: 0,
});

const fetchData = async () => {
  loading.value = true;
  try {
    const isRead = readFilter.value === 'all' ? undefined : readFilter.value === 'read';
    const result  = await getMyCopyRecordsPaged(
      {
        pageIndex: pagination.current ?? 1,
        pageSize: pagination.pageSize ?? 10,
      },
      isRead,
    );

    if (!isMounted.value) return;
    dataSource.value = result.items;
    pagination.total = result.total;
  } catch (err) {
    message.error(err instanceof Error ? err.message : t('approvalWorkspace.queryFailed'));
  } finally {
    loading.value = false;
  }
};

const onTableChange = (pager: TablePaginationConfig) => {
  pagination.current = pager.current;
  pagination.pageSize = pager.pageSize;
  void fetchData();
};

const markRead = async (copyRecordId: string | number) => {
  try {
    await markCopyRecordAsRead(String(copyRecordId));

    if (!isMounted.value) return;
    message.success(t('approvalWorkspace.ccMarkReadOk'));
    await fetchData();

    if (!isMounted.value) return;
  } catch (err) {
    message.error(err instanceof Error ? err.message : t('approvalWorkspace.ccOpFailed'));
  }
};

const viewInstance = (instanceId: string | number) => {
  router.push(`/process/instances/${instanceId}`);
};

onMounted(() => {
  void fetchData();
});

watch(readFilter, () => {
  pagination.current = 1;
  void fetchData();
});
</script>

<style scoped>
/* Scoped styles */
</style>
