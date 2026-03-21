<template>
  <a-space direction="vertical" style="width: 100%" :size="16">
    <a-card :title="t('ai.shortcuts.pageTitle')" :bordered="false">
      <template #extra>
        <a-button type="primary" @click="openCreate">{{ t("ai.shortcuts.newCommand") }}</a-button>
      </template>

      <a-table :columns="columns" :data-source="commands" :loading="loading" row-key="id" :pagination="false">
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'enabled'">
            <a-switch :checked="record.isEnabled" @change="toggleEnabled(record, $event)" />
          </template>
          <template v-if="column.key === 'action'">
            <a-space>
              <a-button type="link" @click="openEdit(record)">{{ t("common.edit") }}</a-button>
              <a-popconfirm :title="t('ai.shortcuts.deleteConfirm')" @confirm="handleDelete(record.id)">
                <a-button type="link" danger>{{ t("common.delete") }}</a-button>
              </a-popconfirm>
            </a-space>
          </template>
        </template>
      </a-table>
    </a-card>

    <OnboardingGuide :shortcuts="commands" />

    <a-modal
      v-model:open="modalOpen"
      :title="editingId ? t('ai.shortcuts.modalEdit') : t('ai.shortcuts.modalCreate')"
      :confirm-loading="submitting"
      @ok="submit"
      @cancel="closeModal"
    >
      <a-form ref="formRef" :model="form" layout="vertical" :rules="rules">
        <a-form-item v-if="!editingId" :label="t('ai.shortcuts.labelCommandKey')" name="commandKey">
          <a-input v-model:value="form.commandKey" />
        </a-form-item>
        <a-form-item :label="t('ai.promptLib.colName')" name="displayName">
          <a-input v-model:value="form.displayName" />
        </a-form-item>
        <a-form-item :label="t('ai.shortcuts.labelTargetPath')" name="targetPath">
          <a-input v-model:value="form.targetPath" />
        </a-form-item>
        <a-form-item :label="t('ai.promptLib.labelDescription')" name="description">
          <a-input v-model:value="form.description" />
        </a-form-item>
        <a-form-item :label="t('ai.shortcuts.labelSort')" name="sortOrder">
          <a-input-number v-model:value="form.sortOrder" :min="0" style="width: 100%" />
        </a-form-item>
      </a-form>
    </a-modal>
  </a-space>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, onUnmounted } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import type { FormInstance } from "ant-design-vue";
import { message } from "ant-design-vue";
import OnboardingGuide from "@/components/ai/OnboardingGuide.vue";
import {
  createAiShortcutCommand,
  deleteAiShortcutCommand,
  getAiShortcutCommands,
  updateAiShortcutCommand,
  type AiShortcutCommandItem
} from "@/services/api-ai-shortcut";

const loading = ref(false);
const commands = ref<AiShortcutCommandItem[]>([]);
const modalOpen = ref(false);
const submitting = ref(false);
const editingId = ref<number | null>(null);
const formRef = ref<FormInstance>();
const form = reactive({
  commandKey: "",
  displayName: "",
  targetPath: "",
  description: "",
  sortOrder: 10
});

const columns = computed(() => [
  { title: t("ai.shortcuts.colCommandKey"), dataIndex: "commandKey", key: "commandKey", width: 180 },
  { title: t("ai.promptLib.colName"), dataIndex: "displayName", key: "displayName", width: 180 },
  { title: t("ai.shortcuts.labelTargetPath"), dataIndex: "targetPath", key: "targetPath" },
  { title: t("ai.shortcuts.labelSort"), dataIndex: "sortOrder", key: "sortOrder", width: 100 },
  { title: t("ai.shortcuts.colEnabled"), key: "enabled", width: 100 },
  { title: t("ai.colActions"), key: "action", width: 140 }
]);

const rules = computed(() => ({
  commandKey: [{ required: true, message: t("ai.shortcuts.ruleCommandKey") }],
  displayName: [{ required: true, message: t("ai.shortcuts.ruleDisplayName") }],
  targetPath: [{ required: true, message: t("ai.shortcuts.ruleTargetPath") }]
}));

async function loadCommands() {
  loading.value = true;
  try {
    commands.value = await getAiShortcutCommands();

    if (!isMounted.value) return;
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.shortcuts.loadFailed"));
  } finally {
    loading.value = false;
  }
}

function openCreate() {
  editingId.value = null;
  Object.assign(form, {
    commandKey: "",
    displayName: "",
    targetPath: "",
    description: "",
    sortOrder: 10
  });
  modalOpen.value = true;
}

function openEdit(command: AiShortcutCommandItem) {
  editingId.value = command.id;
  Object.assign(form, {
    commandKey: command.commandKey,
    displayName: command.displayName,
    targetPath: command.targetPath,
    description: command.description ?? "",
    sortOrder: command.sortOrder
  });
  modalOpen.value = true;
}

function closeModal() {
  modalOpen.value = false;
  formRef.value?.resetFields();
}

async function submit() {
  try {
    await formRef.value?.validate();

    if (!isMounted.value) return;
  } catch {
    return;
  }

  submitting.value = true;
  try {
    if (editingId.value) {
      const current = commands.value.find((x) => x.id === editingId.value);
      await updateAiShortcutCommand(editingId.value, {
        displayName: form.displayName,
        targetPath: form.targetPath,
        description: form.description || undefined,
        sortOrder: form.sortOrder,
        isEnabled: current?.isEnabled ?? true
      });

      if (!isMounted.value) return;
      message.success(t("crud.updateSuccess"));
    } else {
      await createAiShortcutCommand({
        commandKey: form.commandKey,
        displayName: form.displayName,
        targetPath: form.targetPath,
        description: form.description || undefined,
        sortOrder: form.sortOrder
      });

      if (!isMounted.value) return;
      message.success(t("crud.createSuccess"));
    }

    modalOpen.value = false;
    await loadCommands();

    if (!isMounted.value) return;
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.shortcuts.saveFailed"));
  } finally {
    submitting.value = false;
  }
}

async function toggleEnabled(command: AiShortcutCommandItem, checked: boolean) {
  try {
    await updateAiShortcutCommand(command.id, {
      displayName: command.displayName,
      targetPath: command.targetPath,
      description: command.description,
      sortOrder: command.sortOrder,
      isEnabled: checked
    });

    if (!isMounted.value) return;
    await loadCommands();

    if (!isMounted.value) return;
  } catch (error: unknown) {
    message.error((error as Error).message || t("ai.shortcuts.toggleFailed"));
  }
}

async function handleDelete(id: number) {
  try {
    await deleteAiShortcutCommand(id);

    if (!isMounted.value) return;
    message.success(t("crud.deleteSuccess"));
    await loadCommands();

    if (!isMounted.value) return;
  } catch (error: unknown) {
    message.error((error as Error).message || t("crud.deleteFailed"));
  }
}

onMounted(() => {
  void loadCommands();
});
</script>
