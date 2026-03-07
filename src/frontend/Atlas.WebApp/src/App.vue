<template>
  <router-view v-if="isAuthPage" />
  <ConsoleLayout v-else-if="isConsoleRoute" />
  <AppWorkspaceLayout v-else-if="isAppWorkspaceRoute" />
  <MainLayout v-else />
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useRoute } from "vue-router";
import MainLayout from "@/layouts/MainLayout.vue";
import ConsoleLayout from "@/layouts/ConsoleLayout.vue";
import AppWorkspaceLayout from "@/layouts/AppWorkspaceLayout.vue";

const route = useRoute();

const isAuthPage = computed(() => route.path === "/login" || route.path === "/register");
const isConsoleRoute = computed(() => route.path === "/console" || route.path.startsWith("/console/"));
const isAppWorkspaceRoute = computed(() => route.path.startsWith("/apps/"));
</script>