<template>
  <CrudPageLayout :title="t('systemPlugins.pageTitle')">
    <template #toolbar-actions>
      <a-upload
        :show-upload-list="false"
        accept=".atpkg,.zip"
        :before-upload="handleUpload"
      >
        <a-button type="primary" :loading="uploading">
          <UploadOutlined />{{ t("systemPlugins.uploadPackage") }}
        </a-button>
      </a-upload>
      <a-button :loading="reloading" @click="handleReload">
        <ReloadOutlined />{{ t("systemPlugins.reload") }}
      </a-button>
    </template>

    <template #table>
      <a-table
        :columns="columns"
        :data-source="plugins"
        :loading="loading"
        row-key="code"
        :pagination="false"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'state'">
            <a-badge
              :status="stateBadge(record.state)"
              :text="record.state"
            />
          </template>
          <template v-if="column.key === 'category'">
            <a-tag>{{ record.category }}</a-tag>
          </template>
          <template v-if="column.key === 'actions'">
            <a-space>
              <a-button
                v-if="record.state === 'Disabled'"
                size="small"
                type="link"
                @click="handleEnable(record.code)"
              >{{ t("systemPlugins.enable") }}</a-button>
              <a-button
                v-else-if="record.state === 'Loaded'"
                size="small"
                type="link"
                danger
                @click="handleDisable(record.code)"
              >{{ t("systemPlugins.disable") }}</a-button>
              <a-button
                v-if="record.state !== 'Unloaded'"
                size="small"
                type="link"
                danger
                @click="handleUnload(record.code)"
              >{{ t("systemPlugins.unload") }}</a-button>
              <a-button
                size="small"
                type="link"
                @click="openConfig(record)"
              >{{ t("systemPlugins.config") }}</a-button>
            </a-space>
          </template>
        </template>
      </a-table>

      <a-drawer
        v-if="configPlugin"
        :title="t('systemPlugins.configDrawerTitle', { name: configPlugin.name })"
        width="480"
        :open="true"
        @close="configPlugin = null"
      >
        <a-textarea
          v-model:value="configJson"
          :rows="16"
          :placeholder="t('systemPlugins.configPlaceholder')"
        />
        <template #footer>
          <a-space>
            <a-button @click="configPlugin = null">{{ t("common.cancel") }}</a-button>
            <a-button type="primary" :loading="savingConfig" @click="handleSaveConfig">{{ t("common.save") }}</a-button>
          </a-space>
        </template>
      </a-drawer>
    </template>
  </CrudPageLayout>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from "vue";
import { useI18n } from "vue-i18n";

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import { UploadOutlined, ReloadOutlined } from "@ant-design/icons-vue";
import CrudPageLayout from "@/components/crud/CrudPageLayout.vue";
import { message } from "ant-design-vue";
import type { BadgeProps } from "ant-design-vue";
import {
  getInstalledPlugins,
  enablePlugin,
  disablePlugin,
  unloadPlugin,
  reloadPlugins,
  installPluginPackage,
  getPluginConfig,
  savePluginConfig,
} from "@/services/api-plugin";
import type { PluginDescriptor, PluginState } from "@/types/plugin";

const { t } = useI18n();

const loading = ref(false);
const plugins = ref<PluginDescriptor[]>([]);
const reloading = ref(false);
const uploading = ref(false);
const configPlugin = ref<PluginDescriptor | null>(null);
const configJson = ref("");
const savingConfig = ref(false);

const columns = computed(() => [
  { title: t("systemPlugins.colName"), dataIndex: "name", key: "name" },
  { title: t("systemPlugins.colCode"), dataIndex: "code", key: "code" },
  { title: t("systemPlugins.colVersion"), dataIndex: "version", key: "version" },
  { title: t("systemPlugins.colCategory"), key: "category" },
  { title: t("systemPlugins.colAuthor"), dataIndex: "author", key: "author" },
  { title: t("systemPlugins.colState"), key: "state" },
  { title: t("systemPlugins.colActions"), key: "actions", width: 200 },
]);

async function fetchPlugins() {
  loading.value = true;
  try {
    const res = await getInstalledPlugins();
    if (res.success) plugins.value = res.data ?? [];
  } finally {
    loading.value = false;
  }
}

async function handleEnable(code: string) {
  await enablePlugin(code);
  message.success(t("systemPlugins.enabledOk"));
  fetchPlugins();
}

async function handleDisable(code: string) {
  await disablePlugin(code);
  message.success(t("systemPlugins.disabledOk"));
  fetchPlugins();
}

async function handleUnload(code: string) {
  await unloadPlugin(code);
  message.success(t("systemPlugins.unloadedOk"));
  fetchPlugins();
}

async function handleReload() {
  reloading.value = true;
  try {
    await reloadPlugins();
    await fetchPlugins();
    message.success(t("systemPlugins.reloadDone"));
  } finally {
    reloading.value = false;
  }
}

async function handleUpload(file: File) {
  uploading.value = true;
  try {
    const res = await installPluginPackage(file);
    if (res.success) {
      message.success(t("systemPlugins.installSuccess", { code: res.data?.code ?? "" }));
      fetchPlugins();
    }
  } catch {
    message.error(t("systemPlugins.installFailed"));
  } finally {
    uploading.value = false;
  }
  return false;
}

async function openConfig(plugin: PluginDescriptor) {
  configPlugin.value = plugin;
  configJson.value = "{}";
  const res = await getPluginConfig(plugin.code);
  if (res.success && res.data?.configJson) {
    configJson.value = res.data.configJson;
  }
}

async function handleSaveConfig() {
  if (!configPlugin.value) return;
  savingConfig.value = true;
  try {
    await savePluginConfig(configPlugin.value.code, "Global", configJson.value);
    message.success(t("systemPlugins.configSaved"));
    configPlugin.value = null;
  } finally {
    savingConfig.value = false;
  }
}

function stateBadge(state: PluginState): BadgeProps["status"] {
  const map: Record<PluginState, BadgeProps["status"]> = {
    Loaded: "success",
    Disabled: "warning",
    Unloaded: "default",
    Failed: "error",
    NoEntryPoint: "error",
  };
  return map[state] ?? "default";
}

onMounted(fetchPlugins);
</script>
