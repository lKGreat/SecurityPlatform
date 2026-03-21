<template>
  <div class="page-container">
    <div class="page-header">
      <h2 class="page-title">{{ t("notificationInbox.pageTitle") }}</h2>
      <a-button v-if="unreadCount > 0" @click="handleMarkAll">{{ t("notificationInbox.markAllRead") }}</a-button>
    </div>

    <a-tabs v-model:active-key="activeTab" @change="handleTabChange">
      <a-tab-pane key="all" :tab="t('notificationInbox.tabAll')" />
      <a-tab-pane key="unread" :tab="t('notificationInbox.tabUnread', { count: unreadCount })" />
      <a-tab-pane key="read" :tab="t('notificationInbox.tabRead')" />
    </a-tabs>

    <a-spin :spinning="loading">
      <div v-if="items.length > 0" class="notif-list">
        <div
          v-for="item in items"
          :key="item.userNotificationId"
          class="notif-card"
          :class="{ unread: !item.isRead }"
        >
          <div class="notif-card-left">
            <a-tag :color="tagColor(item.noticeType)">{{ typeLabel(item.noticeType) }}</a-tag>
            <a-tag v-if="item.priority === 2" color="red">{{ t("notificationInbox.urgent") }}</a-tag>
            <a-tag v-else-if="item.priority === 1" color="orange">{{ t("notificationInbox.important") }}</a-tag>
          </div>
          <div class="notif-card-body">
            <div class="notif-card-title">
              <span v-if="!item.isRead" class="unread-dot" />
              {{ item.title }}
            </div>
            <div class="notif-card-content">{{ item.content }}</div>
            <div class="notif-card-meta">{{ formatTime(item.publishedAt) }}</div>
          </div>
          <div class="notif-card-right">
            <a-button
              v-if="resolveDeepLink(item)"
              size="small"
              type="link"
              @click="handleOpenDeepLink(item)"
            >{{ t("notificationInbox.goHandle") }}</a-button>
            <a-button
              v-if="!item.isRead"
              size="small"
              type="link"
              @click="handleMarkRead(item)"
            >{{ t("notificationInbox.markRead") }}</a-button>
          </div>
        </div>
      </div>
      <a-empty v-else :description="t('notificationInbox.empty')" />
    </a-spin>

    <div v-if="total > pageSize" class="pagination-bar">
      <a-pagination
        v-model:current="pageIndex"
        :page-size="pageSize"
        :total="total"
        show-quick-jumper
        @change="loadData"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, onUnmounted } from "vue";
import { useI18n } from "vue-i18n";

const isMounted = ref(false);
onMounted(() => { isMounted.value = true; });
onUnmounted(() => { isMounted.value = false; });

import { useRouter } from "vue-router";
import { message } from "ant-design-vue";
import { getMyNotifications, getUnreadCount, markRead, markAllRead } from "@/services/notification";
import type { UserNotificationDto } from "@/services/notification";

const { t, locale } = useI18n();

const loading = ref(false);
const router = useRouter();
const items = ref<UserNotificationDto[]>([]);
const total = ref(0);
const pageIndex = ref(1);
const pageSize = ref(20);
const activeTab = ref<"all" | "unread" | "read">("all");
const unreadCount = ref(0);

const isReadFilter = computed<boolean | undefined>(() => {
  if (activeTab.value === "unread") return false;
  if (activeTab.value === "read") return true;
  return undefined;
});

const loadData = async () => {
  loading.value = true;
  try {
    const [result, count]  = await Promise.all([
      getMyNotifications(pageIndex.value, pageSize.value, isReadFilter.value),
      getUnreadCount()
    ]);

    if (!isMounted.value) return;
    items.value = result.items;
    total.value = result.total;
    unreadCount.value = count;
  } catch (e: unknown) {
    message.error(e instanceof Error ? e.message : t("notificationInbox.loadFailed"));
  } finally {
    loading.value = false;
  }
};

const handleTabChange = () => {
  pageIndex.value = 1;
  loadData();
};

const handleMarkRead = async (item: UserNotificationDto) => {
  try {
    await markRead(item.notificationId);

    if (!isMounted.value) return;
    await loadData();

    if (!isMounted.value) return;
  } catch {
    message.error(t("notificationInbox.opFailed"));
  }
};

const handleMarkAll = async () => {
  try {
    await markAllRead();

    if (!isMounted.value) return;
    await loadData();

    if (!isMounted.value) return;
    message.success(t("notificationInbox.allMarked"));
  } catch {
    message.error(t("notificationInbox.opFailed"));
  }
};

const buildPendingTaskLink = (taskId: string) => `/approval/workspace?tab=pending&taskId=${encodeURIComponent(taskId)}`;

const resolveDeepLink = (item: UserNotificationDto): string | null => {
  const directPath = item.content.match(/(\/process\/tasks\/[A-Za-z0-9\-]+)/);
  if (directPath?.[1]) {
    const taskId = directPath[1].split("/").pop();
    return taskId ? buildPendingTaskLink(taskId) : "/approval/workspace?tab=pending";
  }
  const taskIdMatch = item.content.match(/taskId[:=]\s*([A-Za-z0-9\-]+)/i);
  if (taskIdMatch?.[1]) {
    return buildPendingTaskLink(taskIdMatch[1]);
  }
  return null;
};

const handleOpenDeepLink = (item: UserNotificationDto) => {
  const link = resolveDeepLink(item);
  if (!link) {
    return;
  }
  if (!item.isRead) {
    void markRead(item.notificationId).catch(() => undefined);
  }
  void router.push(link);
};

const typeLabel = (type: string) => {
  const map: Record<string, string> = {
    Announcement: t("notificationInbox.typeAnnouncement"),
    System: t("notificationInbox.typeSystem"),
    Reminder: t("notificationInbox.typeReminder")
  };
  return map[type] ?? type;
};

const tagColor = (type: string) => {
  const map: Record<string, string> = { Announcement: "blue", System: "purple", Reminder: "orange" };
  return map[type] ?? "default";
};

const formatTime = (iso: string) => {
  try {
    const loc = locale.value === "en-US" ? "en-US" : "zh-CN";
    return new Date(iso).toLocaleString(loc);
  } catch {
    return iso;
  }
};

onMounted(loadData);
</script>

<style scoped>
.page-container {
  padding: 24px;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.page-title {
  margin: 0;
  font-size: 18px;
  font-weight: 600;
}

.notif-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 16px;
}

.notif-card {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 16px;
  background: #fff;
  border-radius: 8px;
  border: 1px solid #f0f0f0;
  transition: box-shadow 0.2s;
}

.notif-card:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.notif-card.unread {
  background: #f0f7ff;
  border-color: #91caff;
}

.notif-card-left {
  flex-shrink: 0;
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding-top: 2px;
}

.notif-card-body {
  flex: 1;
  min-width: 0;
}

.notif-card-title {
  font-size: 14px;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 6px;
}

.unread-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #1677ff;
  flex-shrink: 0;
}

.notif-card-content {
  font-size: 13px;
  color: #666;
  margin-top: 4px;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

.notif-card-meta {
  font-size: 12px;
  color: #999;
  margin-top: 6px;
}

.notif-card-right {
  flex-shrink: 0;
}

.pagination-bar {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}
</style>
