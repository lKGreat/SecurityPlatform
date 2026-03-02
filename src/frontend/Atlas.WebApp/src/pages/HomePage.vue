<template>
  <a-card title="系统总览" class="page-card">
    <a-skeleton :loading="loading" active>
      <a-row :gutter="16">
        <a-col :span="6">
          <a-statistic title="资产总量" :value="metrics?.assetsTotal ?? 0" />
        </a-col>
        <a-col :span="6">
          <a-statistic title="今日告警" :value="metrics?.alertsToday ?? 0" />
        </a-col>
        <a-col :span="6">
          <a-statistic title="今日审计" :value="metrics?.auditEventsToday ?? 0" />
        </a-col>
        <a-col :span="6">
          <a-statistic title="运行中实例" :value="metrics?.runningInstances ?? 0" />
        </a-col>
      </a-row>
    </a-skeleton>

    <a-divider>组织与权限</a-divider>
    <a-row :gutter="[16, 16]">
      <a-col v-for="entry in organizationEntries" :key="entry.title" :span="6">
        <a-card hoverable class="quick-card" :title="entry.title" @click="go(entry.path)">
          <p>{{ entry.description }}</p>
        </a-card>
      </a-col>
    </a-row>

    <a-divider>业务与应用</a-divider>
    <a-row :gutter="[16, 16]">
      <a-col v-for="entry in businessEntries" :key="entry.title" :span="6">
        <a-card hoverable class="quick-card" :title="entry.title" @click="go(entry.path)">
          <p>{{ entry.description }}</p>
        </a-card>
      </a-col>
    </a-row>

    <a-divider>安全运营</a-divider>
    <a-row :gutter="[16, 16]">
      <a-col v-for="entry in securityEntries" :key="entry.title" :span="6">
        <a-card hoverable class="quick-card" :title="entry.title" @click="go(entry.path)">
          <p>{{ entry.description }}</p>
        </a-card>
      </a-col>
    </a-row>
  </a-card>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { message } from "ant-design-vue";
import { getVisualizationMetrics } from "@/services/api";
import type { VisualizationMetricsResponse } from "@/types/api";
import { getAuthProfile, hasPermission } from "@/utils/auth";

const router = useRouter();
const loading = ref(false);
const metrics = ref<VisualizationMetricsResponse | null>(null);
const profile = ref(getAuthProfile());

type QuickEntrySource = {
  title: string;
  description: string;
  path: string;
  permissions: string[];
};

const organizationEntriesSource: QuickEntrySource[] = [
  { title: "员工管理", description: "人员信息 / 角色与权限 / 账号状态", path: "/system/users", permissions: ["users:view"] },
  { title: "部门管理", description: "组织层级 / 负责人 / 可见范围", path: "/system/departments", permissions: ["departments:view"] },
  { title: "职位管理", description: "岗位序列 / 影响面提示 / 状态", path: "/system/positions", permissions: ["positions:view"] },
  { title: "角色管理", description: "成员 / 权限 / 数据范围", path: "/system/roles", permissions: ["roles:view"] }
];

const businessEntriesSource: QuickEntrySource[] = [
  { title: "权限管理", description: "功能权限 + 数据权限配置", path: "/system/permissions", permissions: ["permissions:view"] },
  { title: "菜单管理", description: "菜单层级 / 权限绑定 / 隐藏", path: "/system/menus", permissions: ["menus:view"] },
  { title: "项目管理", description: "成员分配 / 数据隔离", path: "/system/projects", permissions: ["projects:view"] },
  { title: "应用配置", description: "可见范围 / 项目模式 / 状态", path: "/system/apps", permissions: ["apps:view"] }
];

const securityEntriesSource: QuickEntrySource[] = [
  { title: "资产中心", description: "资产盘点 / 分类 / 风险定位", path: "/assets", permissions: ["assets:view"] },
  { title: "审计中心", description: "操作留痕 / 风险回溯", path: "/audit", permissions: ["audit:view"] },
  { title: "告警中心", description: "告警聚合 / 处置跟踪", path: "/alert", permissions: ["alert:view"] },
  { title: "审批中心", description: "流程编排 / 审批任务", path: "/approval/flows", permissions: ["approval:flow:view", "approval:flow:create"] }
];

const canAccess = (permissions: string[]) => permissions.some((code) => hasPermission(profile.value, code));

const organizationEntries = computed(() =>
  organizationEntriesSource
    .filter((entry) => canAccess(entry.permissions))
    .map((entry) => ({ title: entry.title, description: entry.description, path: entry.path }))
);

const businessEntries = computed(() =>
  businessEntriesSource
    .filter((entry) => canAccess(entry.permissions))
    .map((entry) => ({ title: entry.title, description: entry.description, path: entry.path }))
);

const securityEntries = computed(() =>
  securityEntriesSource
    .filter((entry) => canAccess(entry.permissions))
    .map((entry) => ({ title: entry.title, description: entry.description, path: entry.path }))
);

const go = (path: string) => router.push(path);

const loadMetrics = async () => {
  if (!hasPermission(profile.value, "visualization:view")) {
    metrics.value = null;
    return;
  }

  loading.value = true;
  try {
    metrics.value = await getVisualizationMetrics();
  } catch (error) {
    message.error((error as Error).message || "加载指标失败");
  } finally {
    loading.value = false;
  }
};

onMounted(loadMetrics);
</script>

<style scoped>
.quick-card {
  min-height: 110px;
}
</style>
