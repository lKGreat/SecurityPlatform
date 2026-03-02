<template>
  <a-breadcrumb class="app-breadcrumb">
    <a-breadcrumb-item v-for="item in items" :key="item.path || item.title">
      <router-link v-if="item.path" :to="item.path">{{ item.title }}</router-link>
      <span v-else>{{ item.title }}</span>
    </a-breadcrumb-item>
  </a-breadcrumb>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useRoute } from "vue-router";

const route = useRoute();

const items = computed(() => {
  const matched = route.matched
    .filter((record) => record.path !== "/" && typeof record.meta?.title === "string")
    .map((record) => ({
      title: String(record.meta?.title),
      path: record.path
    }));
  return matched;
});
</script>
