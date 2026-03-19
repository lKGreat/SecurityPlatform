<template>
  <div class="runtime-executions-page" data-testid="e2e-console-runtime-executions-page">
    <a-card :bordered="false" class="runtime-execution-card">
      <template #title>运行执行记录</template>
      <template #extra>
        <a-input-search
          v-model:value="keyword"
          allow-clear
          placeholder="按 workflowId / 状态 / 错误信息检索"
          style="width: 280px"
          @search="handleSearch"
        />
      </template>

      <a-table
        row-key="id"
        :loading="loading"
        :columns="columns"
        :data-source="rows"
        :pagination="pagination"
        @change="handleTableChange"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'status'">
            <a-tag :color="resolveStatusColor(record.status)">{{ record.status }}</a-tag>
          </template>
          <template v-if="column.key === 'startedAt'">
            {{ formatDate(record.startedAt) }}
          </template>
          <template v-if="column.key === 'completedAt'">
            {{ formatDate(record.completedAt) }}
          </template>
          <template v-if="column.key === 'errorMessage'">
            {{ record.errorMessage || "-" }}
          </template>
          <template v-if="column.key === 'actions'">
            <a-button type="link" size="small" @click="openDetail(record.id)">详情</a-button>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-drawer
      v-model:open="detailVisible"
      title="运行执行详情"
      width="860"
      :destroy-on-close="true"
    >
      <a-spin :spinning="detailLoading">
        <a-descriptions :column="2" bordered size="small">
          <a-descriptions-item label="执行ID">{{ detail?.id || "-" }}</a-descriptions-item>
          <a-descriptions-item label="WorkflowId">{{ detail?.workflowId || "-" }}</a-descriptions-item>
          <a-descriptions-item label="AppId">{{ detail?.appId || "-" }}</a-descriptions-item>
          <a-descriptions-item label="ReleaseId">{{ detail?.releaseId || "-" }}</a-descriptions-item>
          <a-descriptions-item label="RuntimeContextId">{{ detail?.runtimeContextId || "-" }}</a-descriptions-item>
          <a-descriptions-item label="状态">{{ detail?.status || "-" }}</a-descriptions-item>
          <a-descriptions-item label="开始时间">{{ formatDate(detail?.startedAt) }}</a-descriptions-item>
          <a-descriptions-item label="完成时间">{{ formatDate(detail?.completedAt) }}</a-descriptions-item>
          <a-descriptions-item label="错误信息" :span="2">
            {{ detail?.errorMessage || "-" }}
          </a-descriptions-item>
        </a-descriptions>

        <a-divider orientation="left">InputsJson</a-divider>
        <pre class="json-block">{{ detail?.inputsJson || "-" }}</pre>

        <a-divider orientation="left">OutputsJson</a-divider>
        <pre class="json-block">{{ detail?.outputsJson || "-" }}</pre>

        <a-divider orientation="left">审计追踪</a-divider>
        <a-table
          row-key="auditId"
          :loading="auditLoading"
          :columns="auditColumns"
          :data-source="auditRows"
          :pagination="false"
          size="small"
        >
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'occurredAt'">
              {{ formatDate(record.occurredAt) }}
            </template>
          </template>
        </a-table>
      </a-spin>
    </a-drawer>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from "vue";
import type { TableColumnsType, TablePaginationConfig } from "ant-design-vue";
import { message } from "ant-design-vue";
import {
  getRuntimeExecutionAuditTrails,
  getRuntimeExecutionDetail,
  getRuntimeExecutionsPaged
} from "@/services/api-runtime-executions";
import type {
  RuntimeExecutionAuditTrailItem,
  RuntimeExecutionDetail,
  RuntimeExecutionListItem
} from "@/types/platform-v2";

const loading = ref(false);
const detailLoading = ref(false);
const auditLoading = ref(false);
const keyword = ref("");
const rows = ref<RuntimeExecutionListItem[]>([]);
const detail = ref<RuntimeExecutionDetail | null>(null);
const auditRows = ref<RuntimeExecutionAuditTrailItem[]>([]);
const detailVisible = ref(false);
const pageIndex = ref(1);
const pageSize = ref(10);

const columns: TableColumnsType<RuntimeExecutionListItem> = [
  { title: "WorkflowId", dataIndex: "workflowId", key: "workflowId", width: 130 },
  { title: "AppId", dataIndex: "appId", key: "appId", width: 130 },
  { title: "ReleaseId", dataIndex: "releaseId", key: "releaseId", width: 130 },
  { title: "状态", dataIndex: "status", key: "status", width: 110 },
  { title: "开始时间", dataIndex: "startedAt", key: "startedAt", width: 180 },
  { title: "完成时间", dataIndex: "completedAt", key: "completedAt", width: 180 },
  { title: "错误信息", dataIndex: "errorMessage", key: "errorMessage", ellipsis: true },
  { title: "操作", key: "actions", width: 90, fixed: "right" }
];

const auditColumns: TableColumnsType<RuntimeExecutionAuditTrailItem> = [
  { title: "审计ID", dataIndex: "auditId", key: "auditId", width: 150 },
  { title: "操作人", dataIndex: "actor", key: "actor", width: 130 },
  { title: "动作", dataIndex: "action", key: "action", width: 180 },
  { title: "结果", dataIndex: "result", key: "result", width: 120 },
  { title: "目标", dataIndex: "target", key: "target", ellipsis: true },
  { title: "发生时间", dataIndex: "occurredAt", key: "occurredAt", width: 180 }
];

const pagination = ref<TablePaginationConfig>({
  current: 1,
  pageSize: 10,
  total: 0,
  showSizeChanger: true,
  showTotal: (all) => `共 ${all} 条`
});

function formatDate(value?: string) {
  if (!value) {
    return "-";
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return date.toLocaleString();
}

function resolveStatusColor(status: string) {
  if (status === "Completed") {
    return "success";
  }
  if (status === "Failed") {
    return "error";
  }
  return "processing";
}

async function loadRuntimeExecutions() {
  loading.value = true;
  try {
    const result = await getRuntimeExecutionsPaged({
      pageIndex: pageIndex.value,
      pageSize: pageSize.value,
      keyword: keyword.value || undefined
    });
    rows.value = result.items;
    pagination.value = {
      ...pagination.value,
      current: result.pageIndex,
      pageSize: result.pageSize,
      total: result.total
    };
  } catch (error) {
    message.error((error as Error).message || "加载运行执行记录失败");
  } finally {
    loading.value = false;
  }
}

function handleSearch() {
  pageIndex.value = 1;
  void loadRuntimeExecutions();
}

function handleTableChange(page: TablePaginationConfig) {
  pageIndex.value = page.current ?? 1;
  pageSize.value = page.pageSize ?? 10;
  void loadRuntimeExecutions();
}

async function openDetail(id: string) {
  detailVisible.value = true;
  detailLoading.value = true;
  auditLoading.value = true;
  try {
    const [detailResult, auditResult] = await Promise.all([
      getRuntimeExecutionDetail(id),
      getRuntimeExecutionAuditTrails(id, {
        pageIndex: 1,
        pageSize: 20
      })
    ]);
    detail.value = detailResult;
    auditRows.value = auditResult.items;
  } catch (error) {
    message.error((error as Error).message || "加载运行执行详情失败");
  } finally {
    detailLoading.value = false;
    auditLoading.value = false;
  }
}

onMounted(() => {
  void loadRuntimeExecutions();
});
</script>

<style scoped>
.runtime-executions-page {
  padding: 24px;
}

.runtime-execution-card {
  border-radius: 12px;
}

.json-block {
  max-height: 220px;
  overflow: auto;
  white-space: pre-wrap;
  word-break: break-all;
  background: #fafafa;
  border: 1px solid #f0f0f0;
  border-radius: 8px;
  padding: 12px;
}
</style>
