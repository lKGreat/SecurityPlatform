<template>
  <div class="approval-tasks-page">
    <!-- 顶部工具栏 -->
    <div class="page-toolbar">
      <a-space>
        <a-input-search
          v-model:value="keyword"
          allow-clear
          style="width: 240px"
          placeholder="按标题或节点关键词检索"
          @search="fetchData"
        />
        <a-select v-model:value="statusFilter" style="width: 140px" :options="statusOptions" />
        <a-select
          v-model:value="selectedAppId"
          style="width: 200px"
          :loading="appLoading"
          :options="appOptions"
          allow-clear
          show-search
          placeholder="按应用过滤"
          @change="handleAppScopeChange"
        />
        <a-button @click="fetchData">刷新</a-button>
      </a-space>
    </div>

    <!-- 主从布局容器 -->
    <div class="master-detail-container" :class="{ 'has-detail': isDetailVisible }">
      
      <!-- 左侧卡片列表 (Master) -->
      <div class="master-list">
        <a-spin :spinning="loading">
          <div class="task-list">
            <template v-if="dataSource.length > 0">
              <div 
                v-for="item in dataSource" 
                :key="item.id"
                class="task-card"
                :class="{ 'is-active': selectedItem?.id === item.id }"
                @click="selectItem(item)"
              >
                <div class="task-card-header">
                  <span class="task-flow">{{ item.flowName }}</span>
                  <ApprovalStatusTag :status="item.status" />
                </div>
                <div class="task-card-title">{{ item.title }}</div>
                <div class="task-card-meta">
                  <span>当前节点: {{ item.currentNodeName }}</span>
                  <SlaIndicator :remaining-minutes="item.slaRemainingMinutes" />
                </div>
                <div class="task-card-time">{{ formatTime(item.createdAt) }}</div>
              </div>
            </template>
            <a-empty v-else description="暂无审批待办" style="margin-top: 60px;" />
          </div>
          
          <div class="pagination-wrapper" v-if="pagination.total && pagination.total > 0">
            <a-pagination
              v-model:current="pagination.current"
              :total="pagination.total"
              :pageSize="pagination.pageSize"
              size="small"
              @change="onPageChange"
            />
          </div>
        </a-spin>
      </div>

      <!-- 右侧详情面板 (Detail) -->
      <div v-if="isDetailVisible" class="detail-panel">
        <ApprovalTaskDetailPanel
          v-if="selectedItem"
          :task-id="selectedItem.id"
          @close="clearSelection"
          @refresh="fetchDataAndRetainSelection"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref, watch } from "vue";
import { message } from "ant-design-vue";
import { getMyTasksPaged } from "@/services/api";
import { getLowCodeAppsPaged } from "@/services/lowcode";
import type { TablePaginationConfig } from "ant-design-vue";
import { ApprovalTaskStatus, type ApprovalTaskResponse } from "@/types/api";
import { getCurrentAppIdFromStorage, setCurrentAppIdToStorage } from "@/utils/app-context";
import { useMasterDetail } from "@/composables/useMasterDetail";
import ApprovalTaskDetailPanel from "@/components/approval/ApprovalTaskDetailPanel.vue";
import ApprovalStatusTag from "@/components/approval/ApprovalStatusTag.vue";
import SlaIndicator from "@/components/approval/SlaIndicator.vue";

const dataSource = ref<ApprovalTaskResponse[]>([]);
const loading = ref(false);
const appLoading = ref(false);
const keyword = ref("");
const statusFilter = ref<ApprovalTaskStatus | "all">(ApprovalTaskStatus.Pending);
const selectedAppId = ref<string | undefined>(getCurrentAppIdFromStorage() ?? undefined);
const appOptions = ref<Array<{ label: string; value: string }>>([]);
const statusOptions = [
  { label: "全部", value: "all" },
  { label: "待审批", value: ApprovalTaskStatus.Pending },
  { label: "已同意", value: ApprovalTaskStatus.Approved },
  { label: "已驳回", value: ApprovalTaskStatus.Rejected },
  { label: "已取消", value: ApprovalTaskStatus.Canceled }
];
const pagination = reactive<TablePaginationConfig>({
  current: 1,
  pageSize: 10,
  total: 0
});

const { selectedItem, isDetailVisible, selectItem, clearSelection } = useMasterDetail<ApprovalTaskResponse>();

const fetchData = async () => {
  loading.value = true;
  try {
    const statusValue = statusFilter.value === "all" ? undefined : statusFilter.value;
    const result = await getMyTasksPaged({
      pageIndex: pagination.current ?? 1,
      pageSize: pagination.pageSize ?? 10,
      keyword: keyword.value || undefined
    }, statusValue);
    dataSource.value = result.items;
    pagination.total = result.total;
  } catch (err) {
    message.error(err instanceof Error ? err.message : "查询失败");
  } finally {
    loading.value = false;
  }
};

const fetchDataAndRetainSelection = async () => {
  await fetchData();
  if (selectedItem.value) {
    const stillExists = dataSource.value.find(t => t.id === selectedItem.value!.id);
    if (!stillExists) clearSelection();
  }
};

const loadAppOptions = async () => {
  appLoading.value = true;
  try {
    const result = await getLowCodeAppsPaged({ pageIndex: 1, pageSize: 200 });
    appOptions.value = result.items.map((item) => ({
      label: `${item.name} (${item.appKey})`,
      value: item.id
    }));
  } catch (err) {
    message.error(err instanceof Error ? err.message : "加载应用列表失败");
  } finally {
    appLoading.value = false;
  }
};

const handleAppScopeChange = (value: string | undefined) => {
  setCurrentAppIdToStorage(value);
  pagination.current = 1;
  clearSelection();
  fetchData();
};

const onPageChange = (page: number) => {
  pagination.current = page;
  clearSelection();
  fetchData();
};

const formatTime = (value: string) => {
  return new Date(value).toLocaleString([], { month: '2-digit', day: '2-digit', hour: '2-digit', minute:'2-digit' });
};

onMounted(async () => {
  await loadAppOptions();
  await fetchData();
});

watch(statusFilter, () => {
  pagination.current = 1;
  clearSelection();
  fetchData();
});
</script>

<style scoped>
.approval-tasks-page {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 0;
  background: var(--color-bg-base);
}

.page-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 24px;
  background: var(--color-bg-container);
  border-bottom: 1px solid var(--color-border);
}

/* Keep standard flex container */

.master-detail-container {
  flex: 1;
  display: flex;
  overflow: hidden;
  background: var(--color-bg-base);
}

.master-list {
  width: 100%;
  max-width: 100%;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  background: var(--color-bg-base);
  transition: all 0.3s;
}

.has-detail .master-list {
  width: 380px;
  min-width: 380px;
  border-right: 1px solid var(--color-border);
  background: var(--color-bg-container);
}

.detail-panel {
  flex: 1;
  min-width: 0;
  background: var(--color-bg-container);
  box-shadow: -2px 0 8px rgba(0,0,0,0.02);
  z-index: 2;
  overflow: hidden;
}

/* Card List Styles */
.task-list {
  flex: 1;
  overflow-y: auto;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.task-card {
  background: #fff;
  border: 1px solid var(--color-border);
  border-radius: var(--border-radius-md);
  padding: 16px;
  cursor: pointer;
  transition: all 0.2s;
}

.task-card:hover {
  border-color: var(--color-primary);
  box-shadow: var(--shadow-sm);
}

.task-card.is-active {
  background: var(--color-primary-bg);
  border-color: var(--color-primary);
}

.task-card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.task-flow {
  font-size: 13px;
  color: var(--color-text-tertiary);
}

.task-card-title {
  font-size: 15px;
  font-weight: 600;
  color: var(--color-text-primary);
  margin-bottom: 12px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.task-card-meta {
  display: flex;
  justify-content: space-between;
  font-size: 12px;
  color: var(--color-text-secondary);
  margin-bottom: 8px;
}

.sla-ok { color: var(--color-success); }
.sla-error { color: var(--color-error-text); }

.task-card-time {
  font-size: 12px;
  color: var(--color-text-quaternary);
}

.pagination-wrapper {
  padding: 12px 16px;
  border-top: 1px solid var(--color-border);
  background: var(--color-bg-container);
  display: flex;
  justify-content: center;
}

/* Response handling for mobile */
@media screen and (max-width: 768px) {
  .master-detail-container {
    position: relative;
  }
  .has-detail .master-list {
    display: none; /* Hide list on mobile when detail is shown */
  }
}
</style>
