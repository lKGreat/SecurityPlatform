<template>
  <a-card :title="t('ai.variables.pageTitle')" :bordered="false">
    <div class="toolbar">
      <a-space wrap>
        <a-input-search
          v-model:value="keyword"
          :placeholder="t('ai.variables.searchPlaceholder')"
          style="width: 240px"
          @search="loadData"
        />
        <a-select
          v-model:value="scopeFilter"
          style="width: 140px"
          allow-clear
          :placeholder="t('ai.variables.scopePlaceholder')"
          :options="scopeOptions"
          @change="loadData"
        />
        <a-button @click="handleReset">{{ t("ai.variables.reset") }}</a-button>
        <a-button @click="showSystemDefinitions">{{ t("ai.variables.systemVars") }}</a-button>
        <a-button type="primary" @click="openCreate">{{ t("ai.variables.newVariable") }}</a-button>
      </a-space>
    </div>

    <a-table row-key="id" :columns="columns" :data-source="list" :loading="loading" :pagination="false">
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'scope'">
          <a-tag :color="scopeColor(record.scope)">
            {{ scopeLabel(record.scope) }}
          </a-tag>
        </template>
        <template v-if="column.key === 'value'">
          <a-typography-paragraph :ellipsis="{ rows: 1, expandable: true, symbol: t('ai.expand') }">
            {{ record.value || "-" }}
          </a-typography-paragraph>
        </template>
        <template v-if="column.key === 'action'">
          <a-space>
            <a-button type="link" @click="openEdit(record.id)">{{ t("common.edit") }}</a-button>
            <a-popconfirm :title="t('ai.variables.deleteConfirm')" @confirm="handleDelete(record.id)">
              <a-button type="link" danger>{{ t("common.delete") }}</a-button>
            </a-popconfirm>
          </a-space>
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

    <a-modal
      v-model:open="modalOpen"
      :title="editingId ? t('ai.variables.modalEdit') : t('ai.variables.modalCreate')"
      :confirm-loading="modalLoading"
      @ok="submitForm"
      @cancel="closeModal"
    >
      <a-form ref="formRef" :model="form" layout="vertical" :rules="rules">
        <a-form-item label="Key" name="key">
          <a-input v-model:value="form.key" />
        </a-form-item>
        <a-form-item label="Value" name="value">
          <a-textarea v-model:value="form.value" :rows="4" />
        </a-form-item>
        <a-form-item :label="t('ai.variables.labelScope')" name="scope">
          <a-select v-model:value="form.scope" :options="scopeOptions" />
        </a-form-item>
        <a-form-item :label="t('ai.variables.labelScopeId')" name="scopeId">
          <a-input-number v-model:value="form.scopeId" :min="1" style="width: 100%" />
        </a-form-item>
      </a-form>
    </a-modal>

    <a-drawer
      v-model:open="systemDrawerOpen"
      :title="t('ai.variables.drawerSystemTitle')"
      width="680"
    >
      <a-table row-key="key" :data-source="systemVariables" :columns="systemColumns" :pagination="false" />
    </a-drawer>
  </a-card>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, onUnmounted } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import { message } from "ant-design-vue";
import type { FormInstance } from "ant-design-vue";
import {
  createAiVariable,
  deleteAiVariable,
  getAiSystemVariableDefinitions,
  getAiVariableById,
  getAiVariablesPaged,
  updateAiVariable,
  type AiSystemVariableDefinition,
  type AiVariableListItem,
  type AiVariableScope
} from "@/services/api-ai-variable";

const keyword = ref("");
const scopeFilter = ref<AiVariableScope | undefined>(undefined);
const list = ref<AiVariableListItem[]>([]);
const loading = ref(false);
const pageIndex = ref(1);
const pageSize = ref(20);
const total = ref(0);

const columns = computed(() => [
  { title: t("ai.variables.colKey"), dataIndex: "key", key: "key", width: 220 },
  { title: t("ai.variables.colValue"), key: "value" },
  { title: t("ai.variables.colScope"), key: "scope", width: 120 },
  { title: t("ai.variables.colScopeId"), dataIndex: "scopeId", key: "scopeId", width: 120 },
  { title: t("ai.workflow.colUpdatedAt"), dataIndex: "updatedAt", key: "updatedAt", width: 200 },
  { title: t("ai.colActions"), key: "action", width: 140 }
]);

const scopeOptions = [
  { label: "System", value: 0 },
  { label: "Project", value: 1 },
  { label: "Bot", value: 2 }
];

const modalOpen = ref(false);
const modalLoading = ref(false);
const editingId = ref<number | null>(null);
const formRef = ref<FormInstance>();
const form = reactive({
  key: "",
  value: "",
  scope: 0 as AiVariableScope,
  scopeId: undefined as number | undefined
});
const rules = computed(() => ({
  key: [{ required: true, message: t("ai.variables.ruleKey") }],
  scope: [{ required: true, message: t("ai.variables.ruleScope") }]
}));

const systemDrawerOpen = ref(false);
const systemVariables = ref<AiSystemVariableDefinition[]>([]);
const systemColumns = computed(() => [
  { title: t("ai.variables.colKey"), dataIndex: "key", key: "key", width: 200 },
  { title: t("ai.promptLib.colName"), dataIndex: "name", key: "name", width: 150 },
  { title: t("ai.promptLib.labelDescription"), dataIndex: "description", key: "description" },
  { title: t("ai.variables.colDefault"), dataIndex: "defaultValue", key: "defaultValue", width: 150 }
]);

async function loadData() {
  loading.value = true;
  try {
    const result  = await getAiVariablesPaged(
      {
        pageIndex: pageIndex.value,
        pageSize: pageSize.value,
        keyword: keyword.value || undefined
      },
      {
        scope: scopeFilter.value
      }
    );

    if (!isMounted.value) return;
    list.value = result.items;
    total.value = Number(result.total);
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.variables.loadFailed"));
  } finally {
    loading.value = false;
  }
}

function handleReset() {
  keyword.value = "";
  scopeFilter.value = undefined;
  pageIndex.value = 1;
  void loadData();
}

function openCreate() {
  editingId.value = null;
  Object.assign(form, {
    key: "",
    value: "",
    scope: 0 as AiVariableScope,
    scopeId: undefined
  });
  modalOpen.value = true;
}

async function openEdit(id: number) {
  try {
    const detail  = await getAiVariableById(id);

    if (!isMounted.value) return;
    editingId.value = id;
    Object.assign(form, {
      key: detail.key,
      value: detail.value ?? "",
      scope: detail.scope,
      scopeId: detail.scopeId
    });
    modalOpen.value = true;
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.variables.loadDetailFailed"));
  }
}

function closeModal() {
  modalOpen.value = false;
  formRef.value?.resetFields();
}

async function submitForm() {
  try {
    await formRef.value?.validate();

    if (!isMounted.value) return;
  } catch {
    return;
  }

  modalLoading.value = true;
  try {
    const payload = {
      key: form.key,
      value: form.value || undefined,
      scope: form.scope,
      scopeId: form.scopeId
    };

    if (editingId.value) {
      await updateAiVariable(editingId.value, payload);

      if (!isMounted.value) return;
      message.success(t("crud.updateSuccess"));
    } else {
      await createAiVariable(payload);

      if (!isMounted.value) return;
      message.success(t("crud.createSuccess"));
    }

    modalOpen.value = false;
    await loadData();

    if (!isMounted.value) return;
  } catch (error: unknown) {
    message.error((error as Error).message || t("crud.submitFailed"));
  } finally {
    modalLoading.value = false;
  }
}

async function handleDelete(id: number) {
  try {
    await deleteAiVariable(id);

    if (!isMounted.value) return;
    message.success(t("crud.deleteSuccess"));
    await loadData();

    if (!isMounted.value) return;
  } catch (error: unknown) {
    message.error((error as Error).message || t("crud.deleteFailed"));
  }
}

async function showSystemDefinitions() {
  try {
    systemVariables.value = await getAiSystemVariableDefinitions();

    if (!isMounted.value) return;
    systemDrawerOpen.value = true;
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.variables.loadSystemFailed"));
  }
}

function scopeLabel(scope: AiVariableScope) {
  if (scope === 1) return "Project";
  if (scope === 2) return "Bot";
  return "System";
}

function scopeColor(scope: AiVariableScope) {
  if (scope === 1) return "purple";
  if (scope === 2) return "orange";
  return "blue";
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
