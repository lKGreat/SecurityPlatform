<template>
  <router-view v-if="isAuthPage" />
  <a-layout v-else class="app-shell">
    <a-layout-sider collapsible :collapsed="collapsed" @collapse="toggle">
      <div class="brand">Atlas 安全平台</div>
      <SidebarMenu />
    </a-layout-sider>
    <a-layout>
      <a-layout-header class="app-header">
        <div class="header-left">
          <span class="header-title">多租户安全支撑平台</span>
        </div>
        <div class="header-right">
          <a-dropdown trigger="click">
            <a-button type="text">
              <a-space>
                <a-avatar size="small">{{ profileInitials }}</a-avatar>
                <span>{{ profileDisplayName }}</span>
              </a-space>
            </a-button>
            <template #overlay>
              <a-menu>
                <a-menu-item key="profile" @click="openProfile">个人中心</a-menu-item>
                <a-menu-divider />
                <a-menu-item key="logout" @click="logout">退出登录</a-menu-item>
              </a-menu>
            </template>
          </a-dropdown>
        </div>
      </a-layout-header>
      <a-layout-content class="app-content">
        <TagsView />
        <BreadcrumbView />
        <router-view />
      </a-layout-content>
    </a-layout>
  </a-layout>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useUserStore } from "@/stores/user";
import { usePermissionStore } from "@/stores/permission";
import SidebarMenu from "@/components/layout/SidebarMenu.vue";
import TagsView from "@/components/layout/TagsView.vue";
import BreadcrumbView from "@/components/layout/BreadcrumbView.vue";

const route = useRoute();
const router = useRouter();
const userStore = useUserStore();
const permissionStore = usePermissionStore();
const collapsed = ref(false);

const isAuthPage = computed(() => route.path === "/login" || route.path === "/register");
const profileDisplayName = computed(
  () => userStore.profile?.displayName || userStore.profile?.username || "个人中心"
);
const profileInitials = computed(() => {
  const name = profileDisplayName.value;
  return name.slice(0, 2);
});

function toggle(value: boolean) {
  collapsed.value = value;
}

function openProfile() {
  router.push("/profile");
}

async function logout() {
  await userStore.logout();
  permissionStore.reset();
  router.push("/login");
}
</script>

<style scoped>
.brand {
  height: 48px;
  margin: 12px;
  color: #fff;
  font-weight: 600;
  display: flex;
  align-items: center;
}

.app-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #fff;
}

.app-content {
  margin: 12px;
}

.header-right {
  display: flex;
  align-items: center;
}
</style>
