<template>
  <div class="app-org-page">
    <a-page-header :title="t('appsDepartments.pageTitle')" :sub-title="t('appsDepartments.pageSubtitle')" />
    <a-card class="mt12">
      <template #extra>
        <a-space>
          <a-input-search
            v-model:value="keyword"
            style="width: 220px"
            :placeholder="t('appsDepartments.searchPlaceholder')"
            allow-clear
            @search="handleSearch"
          />
          <a-button @click="loadData">{{ t("commonUi.refresh") }}</a-button>
          <a-button type="primary" @click="openCreateModal">{{ t("appsDepartments.newDept") }}</a-button>
        </a-space>
      </template>
      <a-table row-key="id" :columns="columns" :data-source="rows" :loading="loading" :pagination="pagination" @change="handleTableChange">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'parentId'">
            <span>{{ record.parentId ? deptMap[record.parentId]?.name ?? record.parentId : "-" }}</span>
          </template>
          <template v-else-if="column.key === 'actions'">
            <a-space>
              <a-button type="link" size="small" @click="openEditModal(record)">{{ t("common.edit") }}</a-button>
              <a-popconfirm
                :title="t('appsDepartments.deleteConfirm')"
                :ok-text="t('common.delete')"
                :cancel-text="t('common.cancel')"
                @confirm="removeItem(record.id)"
              >
                <a-button type="link" size="small" danger>{{ t("common.delete") }}</a-button>
              </a-popconfirm>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <a-modal
      v-model:open="modalOpen"
      :title="modalTitle"
      :confirm-loading="submitting"
      :ok-text="t('common.save')"
      :cancel-text="t('common.cancel')"
      @ok="submitForm"
    >
      <a-form layout="vertical">
        <a-form-item :label="t('appsDepartments.labelName')" required><a-input v-model:value="form.name" maxlength="64" /></a-form-item>
        <a-form-item :label="t('appsDepartments.labelCode')" required><a-input v-model:value="form.code" maxlength="64" /></a-form-item>
        <a-form-item :label="t('appsDepartments.labelParent')">
          <a-select
            v-model:value="form.parentId"
            allow-clear
            :placeholder="t('appsDepartments.parentPlaceholder')"
            show-search
            :filter-option="(input: string, option: { label?: string | number }) => option?.label?.toString().toLowerCase().includes(input.toLowerCase()) ?? false"
            :options="parentOptions"
          />
        </a-form-item>
        <a-form-item :label="t('appsDepartments.labelSort')">
          <a-input-number v-model:value="form.sortOrder" :min="0" :max="9999" style="width: 100%" />
        </a-form-item>
      </a-form>
    </a-modal>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { useI18n } from "vue-i18n";
import type { TableColumnsType, TablePaginationConfig } from "ant-design-vue";
import { message } from "ant-design-vue";
import { useRoute } from "vue-router";
import { getAppDepartmentAll, createAppDepartment, updateAppDepartment, deleteAppDepartment, getAppDepartmentsPaged } from "@/services/api-app-members";
import type { AppDepartmentListItem } from "@/types/platform-v2";

const { t } = useI18n();
const route = useRoute();
const appId = computed(() => String(route.params.appId ?? ""));
const loading = ref(false);
const submitting = ref(false);
const keyword = ref("");
const rows = ref<AppDepartmentListItem[]>([]);
const allDepts = ref<AppDepartmentListItem[]>([]);
const pagination = reactive<TablePaginationConfig>({ current: 1, pageSize: 20, total: 0, showSizeChanger: true });
const modalOpen = ref(false);
const editingId = ref<string | null>(null);
const form = reactive({ name: "", code: "", parentId: undefined as number | undefined, sortOrder: 0 });

const modalTitle = computed(() => (editingId.value ? t("appsDepartments.modalEdit") : t("appsDepartments.modalCreate")));
const deptMap = computed(() => Object.fromEntries(allDepts.value.map(d => [d.id, d])));
const parentOptions = computed(() => allDepts.value
  .filter(d => d.id !== editingId.value)
  .map(d => ({ value: Number(d.id), label: d.name })));

const columns = computed<TableColumnsType<AppDepartmentListItem>>(() => [
  { title: t("appsDepartments.colName"), dataIndex: "name", key: "name" },
  { title: t("appsDepartments.colCode"), dataIndex: "code", key: "code", width: 140 },
  { title: t("appsDepartments.colParent"), key: "parentId", width: 140 },
  { title: t("appsDepartments.colSort"), dataIndex: "sortOrder", key: "sortOrder", width: 80 },
  { title: t("appsDepartments.colActions"), key: "actions", width: 140, fixed: "right" }
]);

async function loadData() {
  if (!appId.value) return;
  loading.value = true;
  try {
    const [result, all] = await Promise.all([
      getAppDepartmentsPaged(appId.value, { pageIndex: Number(pagination.current), pageSize: Number(pagination.pageSize), keyword: keyword.value.trim() || undefined }),
      getAppDepartmentAll(appId.value)
    ]);
    rows.value = result.items;
    pagination.total = result.total;
    allDepts.value = all;
  } catch (error) {
    message.error((error as Error).message || t("appsDepartments.loadFailed"));
  } finally {
    loading.value = false;
  }
}

function handleSearch() { pagination.current = 1; void loadData(); }
function handleTableChange(page: TablePaginationConfig) { pagination.current = page.current ?? 1; pagination.pageSize = page.pageSize ?? 20; void loadData(); }

function openCreateModal() {
  editingId.value = null;
  form.name = ""; form.code = ""; form.parentId = undefined; form.sortOrder = 0;
  modalOpen.value = true;
}

function openEditModal(record: AppDepartmentListItem) {
  editingId.value = record.id;
  form.name = record.name; form.code = record.code;
  form.parentId = record.parentId ? Number(record.parentId) : undefined;
  form.sortOrder = record.sortOrder;
  modalOpen.value = true;
}

async function submitForm() {
  if (!appId.value || !form.name.trim() || !form.code.trim()) { message.warning(t("appsDepartments.fillNameCode")); return; }
  submitting.value = true;
  try {
    if (editingId.value) {
      await updateAppDepartment(appId.value, editingId.value, { name: form.name.trim(), code: form.code.trim(), parentId: form.parentId, sortOrder: form.sortOrder });
    } else {
      await createAppDepartment(appId.value, { name: form.name.trim(), code: form.code.trim(), parentId: form.parentId, sortOrder: form.sortOrder });
    }
    message.success(t("appsDepartments.saveSuccess"));
    modalOpen.value = false;
    await loadData();
  } catch (error) {
    message.error((error as Error).message || t("appsDepartments.saveFailed"));
  } finally {
    submitting.value = false;
  }
}

async function removeItem(id: string) {
  if (!appId.value) return;
  try {
    await deleteAppDepartment(appId.value, id);
    message.success(t("appsDepartments.deleteSuccess"));
    await loadData();
  } catch (error) {
    message.error((error as Error).message || t("appsDepartments.deleteFailed"));
  }
}

onMounted(() => { void loadData(); });
</script>

<style scoped>
.app-org-page { padding: 8px; }
.mt12 { margin-top: 12px; }
</style>
