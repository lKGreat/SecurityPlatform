<template>
  <a-card title="系统总览" class="page-card">
    <a-row :gutter="16">
      <a-col :span="6">
        <a-statistic title="租户" :value="3" />
      </a-col>
      <a-col :span="6">
        <a-statistic title="资产" :value="128" />
      </a-col>
      <a-col :span="6">
        <a-statistic title="告警" :value="5" />
      </a-col>
      <a-col :span="6">
        <a-statistic title="流程" :value="12" />
      </a-col>
    </a-row>

    <a-divider />

    <a-row :gutter="[16, 16]">
      <a-col :span="6">
        <a-card hoverable @click="go('/visualization/center')" title="可视化中心">
          <p>总览 / 风险提示 / 快速入口</p>
        </a-card>
      </a-col>
      <a-col :span="6">
        <a-card hoverable @click="go('/visualization/designer')" title="可视化设计器">
          <p>拖拽画布 / 校验 / 发布</p>
        </a-card>
      </a-col>
      <a-col :span="6">
        <a-card hoverable @click="go('/visualization/runtime')" title="运行态中心">
          <p>实例监控 / 节点耗时 / 异常定位</p>
        </a-card>
      </a-col>
      <a-col :span="6">
        <a-card hoverable @click="go('/visualization/governance')" title="治理中心">
          <p>口径 / 数据源 / 审计留痕</p>
        </a-card>
      </a-col>
    </a-row>
  </a-card>
</template>

<script setup lang="ts">
import { useRouter } from "vue-router";

const router = useRouter();
const go = (path: string) => router.push(path);
</script>
