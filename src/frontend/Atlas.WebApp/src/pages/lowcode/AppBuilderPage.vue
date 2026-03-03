<template>
  <div class="app-builder-page">
    <!-- 左侧页面树 -->
    <div class="builder-sidebar">
      <div class="sidebar-header">
        <a-button size="small" @click="goBack">{{ t("lowcodeBuilder.back") }}</a-button>
        <h3>{{ appDetail?.name ?? t("lowcodeBuilder.loading") }}</h3>
      </div>
      <div class="sidebar-actions">
        <a-button type="primary" size="small" block @click="handleAddPage">{{ t("lowcodeBuilder.createPage") }}</a-button>
        <a-button size="small" block style="margin-top: 8px" @click="openVersionDrawer">{{ t("lowcodeBuilder.versionHistory") }}</a-button>
      </div>
      <div class="page-tree">
        <div
          v-for="page in pages"
          :key="page.id"
          class="page-tree-item"
          :class="{ active: selectedPageId === page.id }"
          @click="selectPage(page.id)"
        >
          <span class="page-icon">{{ pageTypeIcon(page.pageType) }}</span>
          <span class="page-name">{{ page.name }}</span>
          <a-tag v-if="page.isPublished" color="green" size="small">{{ t("lowcodeApp.statusPublished") }}</a-tag>
          <a-dropdown trigger="click" @click.stop>
            <a-button type="text" size="small">...</a-button>
            <template #overlay>
              <a-menu>
                <a-menu-item key="edit" @click="handleEditPage(page)">{{ t("lowcodeBuilder.actionEditInfo") }}</a-menu-item>
                <a-menu-item v-if="!page.isPublished" key="publish" @click="handlePublishPage(page.id)">{{ t("lowcodeBuilder.actionPublish") }}</a-menu-item>
                <a-menu-item key="delete" danger @click="handleDeletePage(page.id)">{{ t("lowcodeBuilder.actionDelete") }}</a-menu-item>
              </a-menu>
            </template>
          </a-dropdown>
        </div>
        <div v-if="pages.length === 0 && !loading" class="empty-hint">
          {{ t("lowcodeBuilder.emptyPageHint") }}
        </div>
      </div>
    </div>

    <!-- 右侧设计器区域 -->
    <div class="builder-main">
      <template v-if="selectedPageId && currentSchema">
        <div class="main-toolbar">
          <span class="page-title">{{ currentPageName }}</span>
          <div class="main-toolbar-actions">
            <a-button @click="handleSavePageSchema" :loading="saving">{{ t("lowcodeBuilder.save") }}</a-button>
            <a-button type="primary" @click="handlePublishPage(selectedPageId!)" :loading="publishing">{{ t("lowcodeBuilder.publish") }}</a-button>
          </div>
        </div>
        <AmisEditor
          ref="pageEditorRef"
          :schema="currentSchema"
          height="calc(100vh - 112px)"
          @change="handlePageSchemaChange"
        />
      </template>
      <template v-else>
        <div class="empty-main">
          <p>{{ t("lowcodeBuilder.emptyMainHint") }}</p>
        </div>
      </template>
    </div>

    <!-- 新建/编辑页面对话框 -->
    <a-modal
      v-model:open="pageFormVisible"
      :title="pageFormMode === 'create' ? t('lowcodeBuilder.createPageModalTitle') : t('lowcodeBuilder.editPageModalTitle')"
      :ok-text="t('common.confirm')"
      :cancel-text="t('common.cancel')"
      @ok="handlePageFormSubmit"
    >
      <a-form layout="vertical">
        <a-form-item v-if="pageFormMode === 'create'" :label="t('lowcodeBuilder.pageKeyLabel')" required>
          <a-input v-model:value="pageForm.pageKey" :placeholder="t('lowcodeBuilder.pageKeyPlaceholder')" />
        </a-form-item>
        <a-form-item :label="t('lowcodeBuilder.pageNameLabel')" required>
          <a-input v-model:value="pageForm.name" :placeholder="t('lowcodeBuilder.pageNamePlaceholder')" />
        </a-form-item>
        <a-form-item :label="t('lowcodeBuilder.pageTypeLabel')" required>
          <a-select v-model:value="pageForm.pageType">
            <a-select-option value="List">{{ t("lowcodeBuilder.pageTypeList") }}</a-select-option>
            <a-select-option value="Form">{{ t("lowcodeBuilder.pageTypeForm") }}</a-select-option>
            <a-select-option value="Detail">{{ t("lowcodeBuilder.pageTypeDetail") }}</a-select-option>
            <a-select-option value="Dashboard">{{ t("lowcodeBuilder.pageTypeDashboard") }}</a-select-option>
            <a-select-option value="Blank">{{ t("lowcodeBuilder.pageTypeBlank") }}</a-select-option>
          </a-select>
        </a-form-item>
        <a-form-item :label="t('lowcodeBuilder.routePathLabel')">
          <a-input v-model:value="pageForm.routePath" :placeholder="t('lowcodeBuilder.routePathPlaceholder')" />
        </a-form-item>
        <a-form-item :label="t('lowcodeBuilder.pageDescriptionLabel')">
          <a-textarea v-model:value="pageForm.description" :rows="2" />
        </a-form-item>
        <a-form-item :label="t('lowcodeBuilder.sortOrderLabel')">
          <a-input-number v-model:value="pageForm.sortOrder" :min="0" style="width: 100%" />
        </a-form-item>
      </a-form>
    </a-modal>

    <a-drawer
      v-model:open="versionDrawerVisible"
      :title="t('lowcodeBuilder.versionDrawerTitle')"
      placement="right"
      width="720"
      :destroy-on-close="true"
    >
      <a-table
        :columns="versionColumns"
        :data-source="versionItems"
        :loading="versionLoading"
        row-key="id"
        :pagination="{
          total: versionTotal,
          current: versionPageIndex,
          pageSize: versionPageSize,
          showQuickJumper: true,
          onChange: onVersionPageChange
        }"
      >
        <template #bodyCell="{ column, record }">
          <template v-if="column.key === 'actionType'">
            <a-tag :color="record.actionType === 'Publish' ? 'blue' : 'orange'">
              {{ record.actionType }}
            </a-tag>
          </template>
          <template v-else-if="column.key === 'createdAt'">
            {{ formatTime(record.createdAt) }}
          </template>
          <template v-else-if="column.key === 'sourceVersionId'">
            {{ record.sourceVersionId ?? "-" }}
          </template>
          <template v-else-if="column.key === 'note'">
            {{ record.note ?? "-" }}
          </template>
          <template v-else-if="column.key === 'actions'">
            <a-popconfirm
              :title="t('lowcodeBuilder.rollbackConfirm')"
              :ok-text="t('lowcodeBuilder.rollback')"
              :cancel-text="t('common.cancel')"
              @confirm="handleRollbackVersion(record.id)"
            >
              <a-button
                type="link"
                size="small"
                :loading="rollbackingVersionId === record.id"
              >
                {{ t("lowcodeBuilder.rollback") }}
              </a-button>
            </a-popconfirm>
          </template>
        </template>
      </a-table>
    </a-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { message } from "ant-design-vue";
import { useI18n } from "vue-i18n";
import AmisEditor from "@/components/amis/AmisEditor.vue";
import type { LowCodeAppDetail, LowCodeAppVersionListItem, LowCodePageListItem } from "@/types/lowcode";
import {
  getLowCodeAppDetail,
  getLowCodeAppVersionsPaged,
  rollbackLowCodeAppVersion,
  createLowCodePage,
  updateLowCodePage,
  updateLowCodePageSchema,
  publishLowCodePage,
  deleteLowCodePage
} from "@/services/lowcode";

const route = useRoute();
const router = useRouter();
const { t } = useI18n();
const appId = route.params.id as string;

const loading = ref(true);
const saving = ref(false);
const publishing = ref(false);
const appDetail = ref<LowCodeAppDetail | null>(null);
const pages = ref<LowCodePageListItem[]>([]);
const selectedPageId = ref<string | null>(null);
const pageEditorRef = ref<InstanceType<typeof AmisEditor> | null>(null);
const versionDrawerVisible = ref(false);
const versionLoading = ref(false);
const versionItems = ref<LowCodeAppVersionListItem[]>([]);
const versionTotal = ref(0);
const versionPageIndex = ref(1);
const versionPageSize = ref(10);
const rollbackingVersionId = ref<string | null>(null);
const versionColumns = computed(() => [
  { title: t("lowcodeBuilder.version"), dataIndex: "version", key: "version", width: 100 },
  { title: t("lowcodeBuilder.actionType"), key: "actionType", width: 110 },
  { title: t("lowcodeBuilder.sourceVersion"), key: "sourceVersionId", width: 150 },
  { title: t("lowcodeBuilder.note"), key: "note" },
  { title: t("lowcodeBuilder.createdAt"), key: "createdAt", width: 170 },
  { title: t("lowcodeBuilder.createdBy"), dataIndex: "createdBy", key: "createdBy", width: 100 },
  { title: t("common.actions"), key: "actions", width: 90 }
]);

// Page schemas cache
const pageSchemas = ref<Record<string, Record<string, unknown>>>({});

const currentSchema = computed(() => {
  if (!selectedPageId.value) return null;
  return pageSchemas.value[selectedPageId.value] ?? null;
});

const currentPageName = computed(() => {
  return pages.value.find(p => p.id === selectedPageId.value)?.name ?? "";
});

const pageFormVisible = ref(false);
const pageFormMode = ref<"create" | "edit">("create");
const editingPageId = ref<string | null>(null);
const pageForm = reactive({
  pageKey: "",
  name: "",
  pageType: "List",
  routePath: "",
  description: "",
  sortOrder: 0
});

const pageTypeIcon = (type: string) => {
  const icons: Record<string, string> = {
    List: "T",
    Form: "F",
    Detail: "D",
    Dashboard: "B",
    Blank: "P"
  };
  return icons[type] ?? "P";
};

const generateDefaultSchema = (pageType: string, pageName: string): Record<string, unknown> => {
  switch (pageType) {
    case "List":
      return {
        type: "page",
        title: pageName,
        body: [{
          type: "crud",
          api: "/api/v1/dynamic-tables",
          columns: [
            { name: "id", label: "ID" },
            { name: "name", label: "名称" }
          ]
        }]
      };
    case "Form":
      return {
        type: "page",
        title: pageName,
        body: [{
          type: "form",
          api: "/api/v1/dynamic-tables",
          body: [
            { type: "input-text", name: "name", label: "名称", required: true }
          ]
        }]
      };
    case "Dashboard":
      return {
        type: "page",
        title: pageName,
        body: [{
          type: "grid",
          columns: [
            { body: [{ type: "card", header: { title: "统计卡片" }, body: "数据加载中..." }] },
            { body: [{ type: "card", header: { title: "统计卡片" }, body: "数据加载中..." }] }
          ]
        }]
      };
    default:
      return { type: "page", title: pageName, body: [] };
  }
};

const loadApp = async () => {
  loading.value = true;
  try {
    const detail = await getLowCodeAppDetail(appId);
    appDetail.value = detail;
    pages.value = detail.pages;
  } catch (error) {
    message.error((error as Error).message || t("lowcodeBuilder.loadAppFailed"));
  } finally {
    loading.value = false;
  }
};

const selectPage = async (pageId: string) => {
  selectedPageId.value = pageId;

  if (!pageSchemas.value[pageId]) {
    // Find page in pages list; schema is loaded from app detail
    const page = pages.value.find(p => p.id === pageId);
    if (page) {
      // Load full detail from app (schema is not in list item, need to reload)
      try {
        const detail = await getLowCodeAppDetail(appId);
        appDetail.value = detail;
        pages.value = detail.pages;
        // For now, set a default schema based on page type
        pageSchemas.value[pageId] = generateDefaultSchema(page.pageType, page.name);
      } catch {
        pageSchemas.value[pageId] = { type: "page", title: page.name, body: [] };
      }
    }
  }
};

const handleAddPage = () => {
  pageFormMode.value = "create";
  editingPageId.value = null;
  pageForm.pageKey = "";
  pageForm.name = "";
  pageForm.pageType = "List";
  pageForm.routePath = "";
  pageForm.description = "";
  pageForm.sortOrder = pages.value.length;
  pageFormVisible.value = true;
};

const handleEditPage = (page: LowCodePageListItem) => {
  pageFormMode.value = "edit";
  editingPageId.value = page.id;
  pageForm.pageKey = page.pageKey;
  pageForm.name = page.name;
  pageForm.pageType = page.pageType;
  pageForm.routePath = page.routePath ?? "";
  pageForm.description = page.description ?? "";
  pageForm.sortOrder = page.sortOrder;
  pageFormVisible.value = true;
};

const handlePageFormSubmit = async () => {
  if (!pageForm.name.trim()) {
    message.warning(t("lowcodeBuilder.pageNameRequired"));
    return;
  }

  try {
    if (pageFormMode.value === "create") {
      if (!pageForm.pageKey.trim()) {
        message.warning(t("lowcodeBuilder.pageKeyRequired"));
        return;
      }
      const schema = generateDefaultSchema(pageForm.pageType, pageForm.name);
      await createLowCodePage(appId, {
        pageKey: pageForm.pageKey,
        name: pageForm.name,
        pageType: pageForm.pageType,
        schemaJson: JSON.stringify(schema),
        routePath: pageForm.routePath || undefined,
        description: pageForm.description || undefined,
        sortOrder: pageForm.sortOrder
      });
      message.success(t("lowcodeBuilder.createPageSuccess"));
    } else if (editingPageId.value) {
      const currentSchema = pageSchemas.value[editingPageId.value]
        ?? generateDefaultSchema(pageForm.pageType, pageForm.name);
      await updateLowCodePage(editingPageId.value, {
        name: pageForm.name,
        pageType: pageForm.pageType,
        schemaJson: JSON.stringify(currentSchema),
        routePath: pageForm.routePath || undefined,
        description: pageForm.description || undefined,
        sortOrder: pageForm.sortOrder
      });
      message.success(t("lowcodeBuilder.updatePageSuccess"));
    }
    pageFormVisible.value = false;
    await loadApp();
  } catch (error) {
    message.error((error as Error).message || t("lowcodeBuilder.pageOperationFailed"));
  }
};

const handlePageSchemaChange = (newSchema: Record<string, unknown>) => {
  if (selectedPageId.value) {
    pageSchemas.value[selectedPageId.value] = newSchema;
  }
};

const handleSavePageSchema = async () => {
  if (!selectedPageId.value) return;

  saving.value = true;
  try {
    const currentSchema = pageEditorRef.value?.getSchema()
      ?? pageSchemas.value[selectedPageId.value];
    if (currentSchema) {
      await updateLowCodePageSchema(selectedPageId.value, JSON.stringify(currentSchema));
      message.success(t("lowcodeBuilder.saveSchemaSuccess"));
    }
  } catch (error) {
    message.error((error as Error).message || t("lowcodeBuilder.saveSchemaFailed"));
  } finally {
    saving.value = false;
  }
};

const handlePublishPage = async (pageId: string) => {
  publishing.value = true;
  try {
    await publishLowCodePage(pageId);
    message.success(t("lowcodeBuilder.publishSuccess"));
    await loadApp();
  } catch (error) {
    message.error((error as Error).message || t("lowcodeBuilder.publishFailed"));
  } finally {
    publishing.value = false;
  }
};

const handleDeletePage = async (pageId: string) => {
  try {
    await deleteLowCodePage(pageId);
    if (selectedPageId.value === pageId) {
      selectedPageId.value = null;
    }
    delete pageSchemas.value[pageId];
    message.success(t("lowcodeBuilder.deleteSuccess"));
    await loadApp();
  } catch (error) {
    message.error((error as Error).message || t("lowcodeBuilder.deleteFailed"));
  }
};

const loadVersions = async () => {
  versionLoading.value = true;
  try {
    const result = await getLowCodeAppVersionsPaged(appId, {
      pageIndex: versionPageIndex.value,
      pageSize: versionPageSize.value
    });
    versionItems.value = result.items;
    versionTotal.value = result.total;
  } catch (error) {
    message.error((error as Error).message || t("lowcodeBuilder.loadVersionFailed"));
  } finally {
    versionLoading.value = false;
  }
};

const openVersionDrawer = async () => {
  versionDrawerVisible.value = true;
  versionPageIndex.value = 1;
  await loadVersions();
};

const onVersionPageChange = (page: number, pageSizeValue: number) => {
  versionPageIndex.value = page;
  versionPageSize.value = pageSizeValue;
  loadVersions();
};

const handleRollbackVersion = async (versionId: string) => {
  rollbackingVersionId.value = versionId;
  try {
    const newVersion = await rollbackLowCodeAppVersion(appId, versionId);
    message.success(t("lowcodeBuilder.rollbackSuccess", { version: newVersion }));
    await Promise.all([loadApp(), loadVersions()]);
  } catch (error) {
    message.error((error as Error).message || t("lowcodeBuilder.rollbackFailed"));
  } finally {
    rollbackingVersionId.value = null;
  }
};

const formatTime = (time: string) => {
  try {
    return new Date(time).toLocaleString("zh-CN");
  } catch {
    return time;
  }
};

const goBack = () => {
  router.push({ name: "app-list" });
};

onMounted(() => {
  loadApp();
});
</script>

<style scoped>
.app-builder-page {
  display: flex;
  height: 100vh;
  overflow: hidden;
}

.builder-sidebar {
  width: 260px;
  background: #fff;
  border-right: 1px solid #e8e8e8;
  display: flex;
  flex-direction: column;
  flex-shrink: 0;
}

.sidebar-header {
  padding: 12px 16px;
  border-bottom: 1px solid #f0f0f0;
  display: flex;
  align-items: center;
  gap: 8px;
}

.sidebar-header h3 {
  margin: 0;
  font-size: 14px;
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.sidebar-actions {
  padding: 8px 16px;
}

.page-tree {
  flex: 1;
  overflow-y: auto;
  padding: 4px 8px;
}

.page-tree-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 12px;
  border-radius: 4px;
  cursor: pointer;
  transition: background 0.2s;
}

.page-tree-item:hover {
  background: #f5f5f5;
}

.page-tree-item.active {
  background: #e6f7ff;
  color: #1890ff;
}

.page-icon {
  width: 24px;
  height: 24px;
  border-radius: 4px;
  background: #f0f0f0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  font-weight: 600;
  color: #666;
  flex-shrink: 0;
}

.page-name {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  font-size: 13px;
}

.empty-hint {
  padding: 24px;
  text-align: center;
  color: #999;
  font-size: 13px;
}

.builder-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.main-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 8px 16px;
  background: #fff;
  border-bottom: 1px solid #e8e8e8;
  height: 48px;
}

.page-title {
  font-size: 14px;
  font-weight: 500;
}

.main-toolbar-actions {
  display: flex;
  gap: 8px;
}

.empty-main {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #999;
  font-size: 16px;
}
</style>
