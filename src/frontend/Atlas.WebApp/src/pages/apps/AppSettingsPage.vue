<template>
  <div class="app-settings-page">
    <a-page-header title="应用设置" sub-title="数据源、共享策略与实体别名将在此集中管理" />
    <a-row :gutter="16" style="margin-top: 12px">
      <a-col :span="8">
        <a-card title="数据源配置" hoverable @click="go('/console/datasources')">
          <p>当前阶段为只读与连接测试入口。</p>
        </a-card>
      </a-col>
      <a-col :span="8">
        <a-card title="共享策略" hoverable @click="go('/lowcode/apps')">
          <p>配置用户/角色/部门是否继承平台。</p>
        </a-card>
      </a-col>
      <a-col :span="8">
        <a-card title="实体别名" hoverable @click="go('/lowcode/apps')">
          <p>配置 user/role/department 的展示别名。</p>
        </a-card>
      </a-col>
    </a-row>
  </div>
</template>

<script setup lang="ts">
import { useRouter } from "vue-router";

const router = useRouter();

function go(path: string) {
  router.push(path);
}
</script>

<style scoped>
.app-settings-page {
  padding: 8px;
}
</style>
