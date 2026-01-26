<template>
  <a-card title="系统总览" class="page-card">
    <a-row :gutter="16">
      <a-col :span="8">
        <a-statistic title="租户" :value="3" />
      </a-col>
      <a-col :span="8">
        <a-statistic title="资产" :value="128" />
      </a-col>
      <a-col :span="8">
        <a-statistic title="告警" :value="5" />
      </a-col>
    </a-row>
  </a-card>
</template>