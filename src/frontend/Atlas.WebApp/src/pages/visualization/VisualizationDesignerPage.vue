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
        <div class="panel-title">画布</div>
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
          <a-divider />
          <a-form-item label="选中节点名称">
            <a-input v-model:value="selectedNodeName" :disabled="!selectedNode" placeholder="选择画布节点" />
          </a-form-item>
          <a-form-item label="责任人">
            <a-input v-model:value="selectedNodeAssignee" :disabled="!selectedNode" />
          </a-form-item>
          <a-form-item label="超时(分钟)">
            <a-input-number v-model:value="selectedNodeTimeout" :disabled="!selectedNode" style="width: 100%" />
          </a-form-item>
          <a-button type="primary" block :disabled="!selectedNode" @click="applyNodeEdit">更新节点</a-button>
        </a-form>
      </div>
    </div>
  </a-card>
</template>

<script setup lang="ts">
import { onBeforeUnmount, onMounted, reactive, ref } from "vue";
import { message } from "ant-design-vue";
import { Graph } from "@antv/x6";
import { Dnd } from "@antv/x6-plugin-dnd";
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
const dndRef = ref<Dnd>();

const nodeLibrary = [
  { label: "开始", color: "#1890ff", type: "start" },
  { label: "审批", color: "#52c41a", type: "approve" },
  { label: "条件", color: "#fa8c16", type: "condition" },
  { label: "抄送", color: "#722ed1", type: "cc" },
  { label: "结束", color: "#595959", type: "end" }
];

const selectedNode = ref<any>();
const selectedNodeName = ref("");
const selectedNodeAssignee = ref("");
const selectedNodeTimeout = ref<number | null>(null);

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
  localStorage.setItem("viz_draft", JSON.stringify(graphRef.value?.toJSON() ?? canvasDefinition));
  message.success("已保存草稿");
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
  dndRef.value = new Dnd({ target: graph, scaled: false });

   graph.on("node:click", ({ node }) => {
    selectedNode.value = node;
    selectedNodeName.value = (node.getData()?.name as string) || (node.getAttrs().label?.text as string) || "";
    selectedNodeAssignee.value = (node.getData()?.assignee as string) || "";
    const timeout = node.getData()?.timeoutMinutes;
    selectedNodeTimeout.value = timeout ?? null;
  });
};

const handleDragStart = (e: DragEvent, item: { label: string; color: string; type: string }) => {
  if (!graphRef.value || !dndRef.value) return;
  const node = graphRef.value.createNode({
    width: 120,
    height: 40,
    attrs: {
      body: { stroke: item.color, fill: "#fff" },
      label: { text: item.label, fill: "#262626" }
    },
    data: { type: item.type }
  });
  dndRef.value.start(node, e);
};

const applyNodeEdit = () => {
  if (!selectedNode.value) return;
  selectedNode.value.setData({
    ...selectedNode.value.getData(),
    name: selectedNodeName.value,
    assignee: selectedNodeAssignee.value,
    timeoutMinutes: selectedNodeTimeout.value ?? undefined
  });
  selectedNode.value.setAttrs({
    label: { text: selectedNodeName.value || selectedNode.value.getAttrs().label?.text }
  });
  message.success("节点已更新");
};

onMounted(() => {
  initGraph();
  const draft = localStorage.getItem("viz_draft");
  if (draft && graphRef.value) {
    graphRef.value.fromJSON(JSON.parse(draft));
  }
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
