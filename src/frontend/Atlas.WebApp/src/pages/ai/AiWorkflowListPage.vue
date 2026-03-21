<template>
  <a-card :title="t('ai.workflow.listTitle')" :bordered="false">
    <div class="toolbar">
      <a-space wrap>
        <a-input-search
          v-model:value="keyword"
          :placeholder="t('ai.workflow.searchPlaceholder')"
          style="width: 260px"
          @search="loadData"
        />
        <a-button type="primary" @click="openCreate">{{ t("ai.workflow.newWorkflow") }}</a-button>
      </a-space>
    </div>

    <a-table row-key="id" :columns="columns" :data-source="list" :loading="loading" :pagination="false">
      <template #bodyCell="{ column, record }">
        <template v-if="column.key === 'status'">
          <a-tag :color="statusColor(record.status)">{{ statusText(record.status) }}</a-tag>
        </template>
        <template v-if="column.key === 'action'">
          <a-space>
            <a-button type="link" @click="goEditor(record.id)">{{ t("ai.workflow.edit") }}</a-button>
            <a-button type="link" @click="handlePublish(record.id)">{{ t("ai.workflow.publish") }}</a-button>
            <a-button type="link" @click="handleCopy(record.id)">{{ t("ai.workflow.copy") }}</a-button>
            <a-popconfirm :title="t('ai.workflow.deleteConfirm')" @confirm="handleDelete(record.id)">
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
      v-model:open="modalVisible"
      :title="t('ai.workflow.modalCreateTitle')"
      :confirm-loading="modalLoading"
      @ok="submitCreate"
      @cancel="closeModal"
    >
      <a-form ref="formRef" :model="form" layout="vertical" :rules="rules">
        <a-form-item :label="t('ai.workflow.colName')" name="name">
          <a-input v-model:value="form.name" />
        </a-form-item>
        <a-form-item :label="t('ai.workflow.labelDescription')">
          <a-textarea v-model:value="form.description" :rows="3" />
        </a-form-item>
      </a-form>
    </a-modal>
  </a-card>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, onUnmounted } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import { useRouter } from "vue-router";
import { message } from "ant-design-vue";
import type { FormInstance } from "ant-design-vue";
import {
  copyAiWorkflow,
  createAiWorkflow,
  deleteAiWorkflow,
  getAiWorkflowsPaged,
  publishAiWorkflow,
  type AiWorkflowDefinitionDto
} from "@/services/api-ai-workflow";

const router = useRouter();
const list = ref<AiWorkflowDefinitionDto[]>([]);
const loading = ref(false);
const keyword = ref("");
const pageIndex = ref(1);
const pageSize = ref(20);
const total = ref(0);

const columns = computed(() => [
  { title: t("ai.workflow.colName"), dataIndex: "name", key: "name" },
  { title: t("ai.workflow.colStatus"), dataIndex: "status", key: "status", width: 120 },
  { title: t("ai.workflow.colVersion"), dataIndex: "publishVersion", key: "publishVersion", width: 100 },
  { title: t("ai.workflow.colUpdatedAt"), dataIndex: "updatedAt", key: "updatedAt", width: 220 },
  { title: t("ai.colActions"), key: "action", width: 280 }
]);

const modalVisible = ref(false);
const modalLoading = ref(false);
const formRef = ref<FormInstance>();
const form = reactive({
  name: "",
  description: ""
});

const rules = computed(() => ({
  name: [{ required: true, message: t("ai.workflow.ruleName") }]
}));

function normalizeStatus(status: number | string) {
  if (typeof status === "number") {
    if (status === 1) return "Published";
    if (status === 2) return "Disabled";
    return "Draft";
  }

  return status;
}

function statusText(status: number | string) {
  return normalizeStatus(status);
}

function statusColor(status: number | string) {
  const normalized = normalizeStatus(status);
  if (normalized === "Published") return "green";
  if (normalized === "Disabled") return "default";
  return "blue";
}

async function loadData() {
  loading.value = true;
  try {
    const result  = await getAiWorkflowsPaged(
      { pageIndex: pageIndex.value, pageSize: pageSize.value },
      keyword.value || undefined
    );

    if (!isMounted.value) return;
    list.value = result.items;
    total.value = Number(result.total);
  } catch (err: unknown) {
    message.error((err as Error).message || t("ai.workflow.loadFailed"));
  } finally {
    loading.value = false;
  }
}

function goEditor(id: number) {
  void router.push(`/ai/workflows/${id}/edit`);
}

function openCreate() {
  Object.assign(form, { name: "", description: "" });
  modalVisible.value = true;
}

function closeModal() {
  modalVisible.value = false;
  formRef.value?.resetFields();
}

async function submitCreate() {
  try {
    await formRef.value?.validate();

    if (!isMounted.value) return;
  } catch {
    return;
  }

  modalLoading.value = true;
  try {
    const id  = await createAiWorkflow({
      name: form.name,
      description: form.description || undefined,
      canvasJson: JSON.stringify({ nodes: [], edges: [] }),
      definitionJson: "{}"
    });

    if (!isMounted.value) return;
    message.success(t("crud.createSuccess"));
    modalVisible.value = false;
    await loadData();

    if (!isMounted.value) return;
    goEditor(id);
  } catch (err: unknown) {
    message.error((err as Error).message || t("ai.workflow.createFailed"));
  } finally {
    modalLoading.value = false;
  }
}

async function handleDelete(id: number) {
  try {
    await deleteAiWorkflow(id);

    if (!isMounted.value) return;
    message.success(t("crud.deleteSuccess"));
    await loadData();

    if (!isMounted.value) return;
  } catch (err: unknown) {
    message.error((err as Error).message || t("crud.deleteFailed"));
  }
}

async function handlePublish(id: number) {
  try {
    await publishAiWorkflow(id);

    if (!isMounted.value) return;
    message.success(t("ai.workflow.publishSuccess"));
    await loadData();

    if (!isMounted.value) return;
  } catch (err: unknown) {
    message.error((err as Error).message || t("ai.workflow.publishFailed"));
  }
}

async function handleCopy(id: number) {
  try {
    await copyAiWorkflow(id);

    if (!isMounted.value) return;
    message.success(t("ai.workflow.copySuccess"));
    await loadData();

    if (!isMounted.value) return;
  } catch (err: unknown) {
    message.error((err as Error).message || t("ai.workflow.copyFailed"));
  }
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
