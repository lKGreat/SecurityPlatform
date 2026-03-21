<template>
  <a-card :bordered="false" class="page-card tab-content-card">
    <FilterToolbar
      :show-refresh="true"
      @refresh="fetchData"
    >
      <a-select
        v-model:value="statusFilter"
        style="width: 140px"
        :options="instanceStatusFilterOptions"
        @change="handleFilterUpdate"
      />
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
        <template v-if="column.key === 'status'">
          <a-tag :color="getStatusColor(record.status)">
            {{ getStatusText(record.status) }}
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
            <a-button type="link" size="small" @click="handleViewDetail(record.id)">{{ t('approvalWorkspace.myRequests.viewDetail') }}</a-button>
            <a-button
              v-if="record.status === 0"
              type="link"
              size="small"
              danger
              @click="handleCancel(record.id)"
            >
              {{ t('common.cancel') }}
            </a-button>
          </a-space>
        </template>
      </template>
    </a-table>

    <a-drawer
      v-model:open="drawerVisible"
      :title="t('approvalWorkspace.myRequests.drawerTitle')"
      placement="right"
      width="600"
      @close="handleDrawerClose"
    >
      <div v-if="instanceDetail">
        <a-descriptions :column="1" bordered>
          <a-descriptions-item :label="t('approvalWorkspace.myRequests.descFlowName')">{{ instanceDetail.flowName || '-' }}</a-descriptions-item>
          <a-descriptions-item :label="t('approvalWorkspace.myRequests.descBusinessKey')">{{ instanceDetail.businessKey }}</a-descriptions-item>
          <a-descriptions-item :label="t('approvalWorkspace.myRequests.descCurrentNode')">{{ instanceDetail.currentNodeName || '-' }}</a-descriptions-item>
          <a-descriptions-item :label="t('approvalWorkspace.myRequests.descSla')">
            <a-tag v-if="instanceDetail.slaRemainingMinutes != null" :color="instanceDetail.slaRemainingMinutes >= 0 ? 'processing' : 'error'">
              {{ formatSla(instanceDetail.slaRemainingMinutes) }}
            </a-tag>
            <span v-else>-</span>
          </a-descriptions-item>
          <a-descriptions-item :label="t('approvalWorkspace.myRequests.descStatus')">
            <a-tag :color="getStatusColor(instanceDetail.status)">
              {{ getStatusText(instanceDetail.status) }}
            </a-tag>
          </a-descriptions-item>
          <a-descriptions-item :label="t('approvalWorkspace.myRequests.descStartedAt')">{{ instanceDetail.startedAt }}</a-descriptions-item>
          <a-descriptions-item v-if="instanceDetail.endedAt" :label="t('approvalWorkspace.myRequests.descEndedAt')">
            {{ instanceDetail.endedAt }}
          </a-descriptions-item>
        </a-descriptions>

        <!-- 动态表业务数据展示 -->
        <template v-if="businessData && businessData.length > 0">
          <a-divider>{{ t('approvalWorkspace.myRequests.dividerBusinessData') }}</a-divider>
          <a-descriptions :column="1" bordered size="small">
            <a-descriptions-item
              v-for="item in businessData"
              :key="item.field"
              :label="item.field"
            >
              {{ item.value ?? '-' }}
            </a-descriptions-item>
          </a-descriptions>
        </template>

        <a-divider>{{ t('approvalWorkspace.myRequests.dividerTaskList') }}</a-divider>
        <a-table
          :columns="taskTableColumns"
          :data-source="taskList"
          :loading="taskLoading"
          row-key="id"
          :pagination="false"
          size="small"
        >
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'status'">
              <a-tag :color="getTaskStatusColor(record.status)">
                {{ getTaskStatusText(record.status) }}
              </a-tag>
            </template>
          </template>
        </a-table>

        <a-divider>{{ t('approvalWorkspace.myRequests.dividerHistory') }}</a-divider>
        <a-timeline>
          <a-timeline-item v-for="event in historyList" :key="event.id">
            <p>{{ event.eventType }}</p>
            <p v-if="event.fromNode || event.toNode" style="color: #999; font-size: 12px">
              {{ event.fromNode }} → {{ event.toNode }}
            </p>
            <p style="color: #999; font-size: 12px">{{ event.occurredAt }}</p>
          </a-timeline-item>
        </a-timeline>
      </div>
    </a-drawer>
  </a-card>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch, onUnmounted } from "vue";
import { useI18n } from "vue-i18n";

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import {
  getMyInstancesPaged,
  getApprovalInstanceById,
  getApprovalTasksByInstance,
  getApprovalInstanceHistory,
  cancelApprovalInstance
} from "@/services/api";
import type { TablePaginationConfig } from "ant-design-vue";
import {
  ApprovalInstanceStatus,
  ApprovalTaskStatus,
  type ApprovalInstanceListItem,
  type ApprovalInstanceResponse,
  type ApprovalTaskResponse,
  type ApprovalHistoryEventResponse
} from "@/types/api";
import { message } from "ant-design-vue";
import FilterToolbar from "@/components/common/FilterToolbar.vue";

const { t } = useI18n();

const props = defineProps<{
  urlKeyword?: string;
  urlStatus?: string;
}>();

const emit = defineEmits<{
  'update-filter': [{keyword: string, status: string}];
}>();

const tableColumns = computed(() => [
  { title: t("approvalWorkspace.myRequests.colFlowName"), dataIndex: "flowName", key: "flowName" },
  { title: t("approvalWorkspace.myRequests.colBusinessKey"), dataIndex: "businessKey", key: "businessKey" },
  { title: t("approvalWorkspace.myRequests.colCurrentNode"), dataIndex: "currentNodeName", key: "currentNodeName" },
  { title: t("approvalWorkspace.myRequests.colSla"), key: "sla" },
  { title: t("approvalWorkspace.myRequests.colStatus"), key: "status" },
  { title: t("approvalWorkspace.myRequests.colStartedAt"), dataIndex: "startedAt", key: "startedAt" },
  { title: t("approvalWorkspace.myRequests.colActions"), key: "action", width: 150 }
]);

const taskTableColumns = computed(() => [
  { title: t("approvalWorkspace.myRequests.taskColTitle"), dataIndex: "title", key: "title" },
  { title: t("approvalWorkspace.myRequests.taskColNodeId"), dataIndex: "nodeId", key: "nodeId" },
  { title: t("approvalWorkspace.myRequests.colStatus"), key: "status" },
  { title: t("approvalWorkspace.myRequests.taskColCreatedAt"), dataIndex: "createdAt", key: "createdAt" }
]);

const dataSource = ref<ApprovalInstanceListItem[]>([]);
const loading = ref(false);
const statusFilter = ref<ApprovalInstanceStatus | "all">((props.urlStatus as unknown as ApprovalInstanceStatus) || "all");
const instanceStatusFilterOptions = computed(() => [
  { label: t("approvalWorkspace.statusAll"), value: "all" },
  { label: t("approvalWorkspace.myRequests.filterRunning"), value: ApprovalInstanceStatus.Running },
  { label: t("approvalWorkspace.myRequests.filterCompleted"), value: ApprovalInstanceStatus.Completed },
  { label: t("approvalWorkspace.myRequests.filterRejected"), value: ApprovalInstanceStatus.Rejected },
  { label: t("approvalWorkspace.myRequests.filterCanceled"), value: ApprovalInstanceStatus.Canceled },
  { label: t("approvalWorkspace.myRequests.filterSuspended"), value: ApprovalInstanceStatus.Suspended },
  { label: t("approvalWorkspace.myRequests.filterDraft"), value: ApprovalInstanceStatus.Draft },
  { label: t("approvalWorkspace.myRequests.filterTimedOut"), value: ApprovalInstanceStatus.TimedOut },
  { label: t("approvalWorkspace.myRequests.filterTerminated"), value: ApprovalInstanceStatus.Terminated },
]);
const pagination = reactive<TablePaginationConfig>({
  current: 1,
  pageSize: 10,
  total: 0,
});

const drawerVisible = ref(false);
const instanceDetail = ref<ApprovalInstanceResponse | null>(null);
const taskList = ref<ApprovalTaskResponse[]>([]);
const historyList = ref<ApprovalHistoryEventResponse[]>([]);
const taskLoading = ref(false);
const businessData = ref<Array<{ field: string; value: string | null }>>([]);

const fetchData = async () => {
  loading.value = true;
  try {
    const statusValue = statusFilter.value === "all" ? undefined : statusFilter.value;
    const result  = await getMyInstancesPaged({
      pageIndex: Number(pagination.current ?? 1),
      pageSize: Number(pagination.pageSize ?? 10)
    }, statusValue);

    if (!isMounted.value) return;
    dataSource.value = result.items;
    pagination.total = result.total;
  } catch (err) {
    message.error(err instanceof Error ? err.message : t("approvalWorkspace.queryFailed"));
  } finally {
    loading.value = false;
  }
};

const onTableChange = (pager: TablePaginationConfig) => {
  pagination.current = pager.current;
  pagination.pageSize = pager.pageSize;
  fetchData();
};

const getStatusColor = (status: ApprovalInstanceStatus) => {
  switch (status) {
    case ApprovalInstanceStatus.Running:
      return "blue";
    case ApprovalInstanceStatus.Completed:
      return "green";
    case ApprovalInstanceStatus.Rejected:
      return "red";
    case ApprovalInstanceStatus.Canceled:
      return "default";
    case ApprovalInstanceStatus.Suspended:
      return "orange";
    case ApprovalInstanceStatus.Draft:
      return "purple";
    case ApprovalInstanceStatus.TimedOut:
      return "volcano";
    case ApprovalInstanceStatus.Terminated:
      return "magenta";
    case ApprovalInstanceStatus.AutoApproved:
      return "cyan";
    case ApprovalInstanceStatus.AutoRejected:
      return "geekblue";
    case ApprovalInstanceStatus.AiProcessing:
      return "processing";
    case ApprovalInstanceStatus.AiManualReview:
      return "gold";
    case ApprovalInstanceStatus.Destroy:
      return "default";
    default:
      return "default";
  }
};

const getStatusText = (status: ApprovalInstanceStatus) => {
  switch (status) {
    case ApprovalInstanceStatus.Running:
      return t("approvalWorkspace.myRequests.instRunning");
    case ApprovalInstanceStatus.Completed:
      return t("approvalWorkspace.myRequests.instCompleted");
    case ApprovalInstanceStatus.Rejected:
      return t("approvalWorkspace.myRequests.instRejected");
    case ApprovalInstanceStatus.Canceled:
      return t("approvalWorkspace.myRequests.instCanceled");
    case ApprovalInstanceStatus.Suspended:
      return t("approvalWorkspace.myRequests.instSuspended");
    case ApprovalInstanceStatus.Draft:
      return t("approvalWorkspace.myRequests.instDraft");
    case ApprovalInstanceStatus.TimedOut:
      return t("approvalWorkspace.myRequests.instTimedOut");
    case ApprovalInstanceStatus.Terminated:
      return t("approvalWorkspace.myRequests.instTerminated");
    case ApprovalInstanceStatus.AutoApproved:
      return t("approvalWorkspace.myRequests.instAutoApproved");
    case ApprovalInstanceStatus.AutoRejected:
      return t("approvalWorkspace.myRequests.instAutoRejected");
    case ApprovalInstanceStatus.AiProcessing:
      return t("approvalWorkspace.myRequests.instAiProcessing");
    case ApprovalInstanceStatus.AiManualReview:
      return t("approvalWorkspace.myRequests.instAiManualReview");
    case ApprovalInstanceStatus.Destroy:
      return t("approvalWorkspace.myRequests.instDestroy");
    default:
      return t("approvalWorkspace.myRequests.instUnknown");
  }
};

const formatSla = (value: number) => {
  const abs = Math.abs(value);
  if (abs >= 60) {
    const hours = Math.floor(abs / 60);
    const minutes = abs % 60;
    return value >= 0
      ? t("approvalWorkspace.myRequests.slaRemainHm", { h: hours, m: minutes })
      : t("approvalWorkspace.myRequests.slaOverHm", { h: hours, m: minutes });
  }
  return value >= 0
    ? t("approvalWorkspace.myRequests.slaRemainM", { m: abs })
    : t("approvalWorkspace.myRequests.slaOverM", { m: abs });
};

const getTaskStatusColor = (status: ApprovalTaskStatus) => {
  switch (status) {
    case ApprovalTaskStatus.Pending:
      return "orange";
    case ApprovalTaskStatus.Approved:
      return "green";
    case ApprovalTaskStatus.Rejected:
      return "red";
    case ApprovalTaskStatus.Canceled:
      return "default";
    default:
      return "default";
  }
};

const getTaskStatusText = (status: ApprovalTaskStatus) => {
  switch (status) {
    case ApprovalTaskStatus.Pending:
      return t("approvalWorkspace.statusPending");
    case ApprovalTaskStatus.Approved:
      return t("approvalWorkspace.statusApproved");
    case ApprovalTaskStatus.Rejected:
      return t("approvalWorkspace.statusRejected");
    case ApprovalTaskStatus.Canceled:
      return t("approvalWorkspace.statusCanceled");
    default:
      return t("approvalWorkspace.myRequests.instUnknown");
  }
};

const handleViewDetail = async (id: string) => {
  drawerVisible.value = true;
  taskLoading.value = true;

  try {
    const [instance, tasks, history]  = await Promise.all([
      getApprovalInstanceById(id),
      getApprovalTasksByInstance(id, { pageIndex: 1, pageSize: 100 }),
      getApprovalInstanceHistory(id, { pageIndex: 1, pageSize: 100 })
    ]);

    if (!isMounted.value) return;

    instanceDetail.value = instance;
    taskList.value = tasks.items;
    historyList.value = history.items;
    
    // 解析业务数据 DataJson
    businessData.value = [];
    if (instance.dataJson) {
      try {
        const parsed = JSON.parse(instance.dataJson);
        if (typeof parsed === "object" && parsed !== null) {
          businessData.value = Object.entries(parsed)
            .filter(([key]) => !key.startsWith("_")) // 排除内部字段
            .map(([field, value]) => ({
              field,
              value: value != null ? String(value) : null
            }));
        }
      } catch {
        // DataJson 解析失败时忽略
      }
    }

  } catch (err) {
    message.error(err instanceof Error ? err.message : "加载详情失败");
  } finally {
    taskLoading.value = false;
  }
};

const handleCancel = async (id: string) => {
  try {
    await cancelApprovalInstance(id);

    if (!isMounted.value) return;
    message.success(t("approvalWorkspace.myRequests.cancelSuccess"));
    fetchData();
  } catch (err) {
    message.error(err instanceof Error ? err.message : t("approvalWorkspace.myRequests.cancelFailed"));
  }
};

const handleDrawerClose = () => {
  drawerVisible.value = false;
  instanceDetail.value = null;
  taskList.value = [];
  historyList.value = [];
  businessData.value = [];
};

const handleFilterUpdate = () => {
  emit('update-filter', { keyword: '', status: String(statusFilter.value) });
  fetchData();
};

onMounted(fetchData);

watch(statusFilter, () => {
  pagination.current = 1;
  fetchData();
});
</script>

<style scoped>
.tab-content-card {
  height: 100%;
  display: flex;
  flex-direction: column;
}
/* Ensure the body of the card takes available space */
:deep(.ant-card-body) {
  flex: 1;
  overflow-y: auto;
}
</style>
