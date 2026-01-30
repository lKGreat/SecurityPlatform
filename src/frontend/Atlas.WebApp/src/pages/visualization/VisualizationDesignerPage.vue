<template>
  <a-card class="page-card" :bordered="false">
    <template #title>流程可视化设计器（骨架）</template>
    <template #extra>
      <a-space>
        <a-button @click="handleSave">保存草稿</a-button>
        <a-button @click="handleValidate">校验</a-button>
        <a-button type="primary" @click="handlePublish">发布</a-button>
      </a-space>
    </template>

    <div class="designer-shell">
      <div class="panel panel-left">
        <div class="panel-title">节点库</div>
        <a-list size="small" bordered :data-source="nodeLibrary">
          <template #renderItem="{ item }">
            <a-list-item class="node-item">
              <a-badge :color="item.color" />
              <span
                class="node-label"
                draggable="true"
                @dragstart="(e) => handleDragStart(e, item)"
              >
                {{ item.label }}
              </span>
            </a-list-item>
          </template>
        </a-list>
      </div>

      <div class="panel panel-center">
        <div class="panel-title">画布（占位）</div>
        <div ref="canvasRef" class="canvas"></div>
      </div>

      <div class="panel panel-right">
        <div class="panel-title">属性配置</div>
        <a-form layout="vertical">
          <a-form-item label="流程名称">
            <a-input v-model:value="processName" placeholder="请输入流程名称" />
          </a-form-item>
          <a-form-item label="版本">
            <a-input-number v-model:value="version" :min="1" :max="999" style="width: 100%" />
          </a-form-item>
          <a-form-item label="备注">
            <a-textarea v-model:value="note" :rows="3" />
          </a-form-item>
        </a-form>
      </div>
    </div>
  </a-card>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, reactive, ref } from "vue";
import { message } from "ant-design-vue";
import { Graph, Addon } from "@antv/x6";
import {
  validateVisualizationProcess,
  publishVisualizationProcess
} from "@/services/api";

const processName = ref("示例流程");
const version = ref(1);
const note = ref("");
const canvasDefinition = reactive<{ nodes: any[]; edges: any[] }>({ nodes: [], edges: [] });
const canvasRef = ref<HTMLDivElement>();
const graphRef = ref<Graph>();
const dndRef = ref<Addon.Dnd>();

const nodeLibrary = [
  { label: "开始", color: "#1890ff" },
  { label: "审批", color: "#52c41a" },
  { label: "条件", color: "#fa8c16" },
  { label: "抄送", color: "#722ed1" },
  { label: "结束", color: "#595959" }
];

const handleValidate = async () => {
  const definitionJson = JSON.stringify(graphRef.value?.toJSON() ?? canvasDefinition);
  const result = await validateVisualizationProcess({ definitionJson });
  if (result.passed) {
    message.success("校验通过");
  } else {
    message.error(`校验失败：${result.errors.join("；")}`);
  }
};

const handlePublish = async () => {
  const definitionJson = JSON.stringify(graphRef.value?.toJSON() ?? canvasDefinition);
  const validateResult = await validateVisualizationProcess({ definitionJson });
  if (!validateResult.passed) {
    message.error("请先修复校验错误再发布");
    return;
  }

  const result = await publishVisualizationProcess({
    processId: processName.value,
    version: version.value,
    note: note.value
  });
  message.success(`已发布：${result.processId} v${result.version}`);
};

const handleSave = () => {
  message.success("已保存草稿（示例占位）");
};

const initGraph = () => {
  if (!canvasRef.value) return;
  const graph = new Graph({
    container: canvasRef.value,
    grid: true,
    panning: true,
    mousewheel: true,
    connecting: {
      snap: true,
      allowBlank: false
    }
  });
  graphRef.value = graph;
  dndRef.value = new Addon.Dnd({ target: graph, scaled: false });
};

const handleDragStart = (e: DragEvent, item: { label: string; color: string }) => {
  if (!graphRef.value || !dndRef.value) return;
  const node = graphRef.value.createNode({
    width: 120,
    height: 40,
    attrs: {
      body: { stroke: item.color, fill: "#fff" },
      label: { text: item.label, fill: "#262626" }
    },
    data: { type: item.label }
  });
  dndRef.value.start(node, e);
};

onMounted(() => {
  initGraph();
});

onBeforeUnmount(() => {
  graphRef.value?.dispose();
});
</script>

<style scoped>
.designer-shell {
  display: grid;
  grid-template-columns: 240px 1fr 300px;
  gap: 12px;
  min-height: 520px;
}

.panel {
  background: #fff;
  border: 1px solid #f0f0f0;
  border-radius: 6px;
  padding: 12px;
  display: flex;
  flex-direction: column;
}

.panel-title {
  font-weight: 600;
  margin-bottom: 8px;
}

.canvas-placeholder {
  color: #8c8c8c;
  background: #fafafa;
}

.canvas {
  flex: 1;
  border: 1px dashed #d9d9d9;
  border-radius: 4px;
  min-height: 520px;
}

.node-item {
  display: flex;
  gap: 8px;
  align-items: center;
}

.node-label {
  font-size: 13px;
}
</style>
