<template>
  <CrudPageLayout
    v-model:keyword="filters.businessKey"
    :title="t('approvalRuntime.instanceManageTitle')"
    :search-placeholder="t('approvalRuntime.searchBusinessKey')"
    @search="fetchData"
  >
    <template #search-filters>
      <a-form-item>
        <a-select
          v-model:value="filters.status"
          style="width: 160px"
          :options="statusOptions"
          allow-clear
          :placeholder="t('approvalRuntime.instanceStatusPlaceholder')"
          @change="fetchData"
        />
      </a-form-item>
    </template>

    <template #table>
    <a-table
      :columns="columns"
      :data-source="dataSource"
      :pagination="pagination"
      :loading="loading"
      row-key="id"
      @change="onTableChange"
    >
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'status'">
          <a-tag :color="statusColor(record.status)">
            {{ statusText(record.status) }}
          </a-tag>
        </template>
        <template v-else-if="column.key === 'sla'">
          <a-tag v-if="record.slaRemainingMinutes != null" :color="record.slaRemainingMinutes >= 0 ? 'processing' : 'error'">
            {{ formatSla(record.slaRemainingMinutes) }}
          </a-tag>
          <span v-else>-</span>
        </template>
        <template v-else-if="column.key === 'action'">
          <a-space>
            <a-button type="link" size="small" @click="viewDetail(record.id)">{{ t('approvalRuntime.detail') }}</a-button>
            <a-button
              v-if="record.status === 0"
              type="link"
              size="small"
              danger
              @click="terminate(record.id)"
            >
              {{ t('approvalRuntime.terminate') }}
            </a-button>
          </a-space>
        </template>
      </template>
    </a-table>
    </template>
  </CrudPageLayout>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, onUnmounted } from 'vue';
import { useI18n } from 'vue-i18n';

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import { useRouter } from 'vue-router';
import { message } from 'ant-design-vue';
import type { TablePaginationConfig } from 'ant-design-vue';
import { ApprovalInstanceStatus, type ApprovalInstanceListItem } from '@/types/api';
import { getAdminInstancesPaged, terminateInstance } from '@/services/api';
import CrudPageLayout from "@/components/crud/CrudPageLayout.vue";

const { t } = useI18n();
const router = useRouter();

const columns = computed(() => [
  { title: t('approvalRuntime.colFlowName'), dataIndex: 'flowName', key: 'flowName' },
  { title: t('approvalRuntime.colBusinessKey'), dataIndex: 'businessKey', key: 'businessKey' },
  { title: t('approvalRuntime.colCurrentNode'), dataIndex: 'currentNodeName', key: 'currentNodeName' },
  { title: 'SLA', key: 'sla' },
  { title: t('approvalRuntime.colInitiator'), dataIndex: 'initiatorUserId', key: 'initiatorUserId' },
  { title: t('approvalRuntime.colStatus'), key: 'status' },
  { title: t('approvalRuntime.colStartedAt'), dataIndex: 'startedAt', key: 'startedAt' },
  { title: t('approvalRuntime.colActions'), key: 'action', width: 180 },
]);

const dataSource = ref<ApprovalInstanceListItem[]>([]);
const loading = ref(false);
const filters = reactive<{ status?: number; businessKey?: string }>({});
const statusOptions = computed(() => [
  { label: t('approvalRuntime.instStatusRunning'), value: ApprovalInstanceStatus.Running },
  { label: t('approvalRuntime.instStatusCompleted'), value: ApprovalInstanceStatus.Completed },
  { label: t('approvalRuntime.instStatusRejected'), value: ApprovalInstanceStatus.Rejected },
  { label: t('approvalRuntime.instStatusCanceled'), value: ApprovalInstanceStatus.Canceled },
]);

const pagination = reactive<TablePaginationConfig>({
  current: 1,
  pageSize: 10,
  total: 0,
  showTotal: (total) => t('crud.totalItems', { total }),
});

const fetchData = async () => {
  loading.value = true;
  try {
    const result  = await getAdminInstancesPaged(
      {
        pageIndex: pagination.current ?? 1,
        pageSize: pagination.pageSize ?? 10,
      },
      {
        status: filters.status,
        businessKey: filters.businessKey,
      },
    );

    if (!isMounted.value) return;
    dataSource.value = result.items;
    pagination.total = result.total;
  } catch (err) {
    message.error(err instanceof Error ? err.message : t('approvalRuntime.queryFailed'));
  } finally {
    loading.value = false;
  }
};

const onTableChange = (pager: TablePaginationConfig) => {
  pagination.current = pager.current;
  pagination.pageSize = pager.pageSize;
  void fetchData();
};

const viewDetail = (instanceId: string | number) => {
  router.push(`/process/instances/${instanceId}`);
};

const terminate = async (instanceId: string | number) => {
  try {
    await terminateInstance(String(instanceId), t('approvalRuntime.terminateAdminReason'));

    if (!isMounted.value) return;
    message.success(t('approvalRuntime.terminateSuccess'));
    await fetchData();

    if (!isMounted.value) return;
  } catch (err) {
    message.error(err instanceof Error ? err.message : t('approvalRuntime.terminateFailed'));
  }
};

const statusText = (status: ApprovalInstanceStatus) => {
  switch (status) {
    case ApprovalInstanceStatus.Running:
      return t('approvalRuntime.instStatusRunning');
    case ApprovalInstanceStatus.Completed:
      return t('approvalRuntime.instStatusCompleted');
    case ApprovalInstanceStatus.Rejected:
      return t('approvalRuntime.instStatusRejected');
    case ApprovalInstanceStatus.Canceled:
      return t('approvalRuntime.instStatusCanceled');
    case ApprovalInstanceStatus.Suspended:
      return t('approvalRuntime.instStatusSuspended');
    case ApprovalInstanceStatus.Draft:
      return t('approvalRuntime.instStatusDraft');
    case ApprovalInstanceStatus.TimedOut:
      return t('approvalRuntime.instStatusTimedOut');
    case ApprovalInstanceStatus.Terminated:
      return t('approvalRuntime.instStatusTerminated');
    case ApprovalInstanceStatus.AutoApproved:
      return t('approvalRuntime.instStatusAutoApproved');
    case ApprovalInstanceStatus.AutoRejected:
      return t('approvalRuntime.instStatusAutoRejected');
    case ApprovalInstanceStatus.AiProcessing:
      return t('approvalRuntime.instStatusAiProcessing');
    case ApprovalInstanceStatus.AiManualReview:
      return t('approvalRuntime.instStatusAiManual');
    case ApprovalInstanceStatus.Destroy:
      return t('approvalRuntime.instStatusDestroy');
    default:
      return t('approvalRuntime.instStatusUnknown');
  }
};

const statusColor = (status: ApprovalInstanceStatus) => {
  switch (status) {
    case ApprovalInstanceStatus.Running:
      return 'processing';
    case ApprovalInstanceStatus.Completed:
      return 'success';
    case ApprovalInstanceStatus.Rejected:
      return 'error';
    case ApprovalInstanceStatus.Canceled:
      return 'default';
    case ApprovalInstanceStatus.Suspended:
      return 'orange';
    case ApprovalInstanceStatus.Draft:
      return 'purple';
    case ApprovalInstanceStatus.TimedOut:
      return 'volcano';
    case ApprovalInstanceStatus.Terminated:
      return 'magenta';
    case ApprovalInstanceStatus.AutoApproved:
      return 'cyan';
    case ApprovalInstanceStatus.AutoRejected:
      return 'geekblue';
    case ApprovalInstanceStatus.AiProcessing:
      return 'processing';
    case ApprovalInstanceStatus.AiManualReview:
      return 'gold';
    case ApprovalInstanceStatus.Destroy:
      return 'default';
    default:
      return 'default';
  }
};

const formatSla = (value: number) => {
  const abs = Math.abs(value);
  if (abs >= 60) {
    const hours = Math.floor(abs / 60);
    const minutes = abs % 60;
    return value >= 0
      ? t('approvalRuntime.slaRemain', { h: hours, m: minutes })
      : t('approvalRuntime.slaOver', { h: hours, m: minutes });
  }
  return value >= 0 ? t('approvalRuntime.slaRemainM', { m: abs }) : t('approvalRuntime.slaOverM', { m: abs });
};

onMounted(() => {
  void fetchData();
});
</script>
