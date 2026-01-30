import type {
  ApprovalFlowTree,
  TreeNode,
  StartNode,
  ApproveNode,
  CopyNode,
  ConditionNode,
  EndNode,
  ConditionBranch,
  DynamicConditionNode,
  ParallelConditionNode,
  ParallelNode
} from '@/types/approval-tree';
import type {
  ApprovalDefinitionJson,
  ApprovalDefinitionMeta,
  LfFormPayload,
  ApprovalNode,
  ConditionBranch as DefinitionBranch
} from '@/types/approval-definition';
import { nanoid } from 'nanoid';

export class ApprovalTreeConverter {
  /**
   * 树结构 → X6 图结构（用于渲染或保存）
   */
  static treeToGraph(tree: ApprovalFlowTree): { nodes: any[], edges: any[] } {
    const nodes: any[] = [];
    const edges: any[] = [];
    
    // 递归遍历树节点，生成 X6 节点和边
    this.traverseTree(tree.rootNode, null, nodes, edges, { x: 100, y: 100 });
    
    return { nodes, edges };
  }
  
  /**
   * X6 图结构 → 树结构（从图反推树，用于编辑现有流程）
   */
  static graphToTree(nodes: any[], edges: any[]): ApprovalFlowTree {
    if (!nodes || nodes.length === 0) {
      // 返回默认空树
      return this.createDefaultTree();
    }

    // 找到开始节点
    const startNodeData = nodes.find(n => n.data?.type === 'start' || n.type === 'start');
    if (!startNodeData) {
       console.warn('缺少开始节点, 返回默认树');
       return this.createDefaultTree();
    }
    
    // 构建邻接表方便查找
    const outgoing = new Map<string, any[]>();
    edges.forEach(edge => {
      const source = edge.source?.cell || edge.source;
      if (!outgoing.has(source)) outgoing.set(source, []);
      outgoing.get(source)?.push(edge);
    });

    // 递归构建树
    const rootNode = this.buildTreeFromNode(startNodeData.id, nodes, outgoing);
    
    return {
      flowName: '',
      rootNode: rootNode as StartNode
    };
  }

  static createDefaultTree(): ApprovalFlowTree {
    return {
      flowName: '',
      rootNode: {
        id: nanoid(),
        nodeType: 'start',
        nodeName: '发起人',
        childNode: {
          id: nanoid(),
          nodeType: 'end',
          nodeName: '结束'
        }
      }
    };
  }
  
  /**
   * 树结构 → 后端 definitionJson（nodes + edges 格式）
   */
  static treeToDefinitionJson(
    tree: ApprovalFlowTree,
    meta?: ApprovalDefinitionMeta,
    lfForm?: LfFormPayload
  ): string {
    const definition: ApprovalDefinitionJson = {
      meta: {
        flowName: meta?.flowName ?? tree.flowName,
        description: meta?.description,
        category: meta?.category,
        visibilityScope: meta?.visibilityScope,
        isQuickEntry: meta?.isQuickEntry,
        isLowCodeFlow: meta?.isLowCodeFlow
      },
      lfForm,
      nodes: {
        rootNode: this.treeNodeToDefinition(tree.rootNode)
      }
    };
    return JSON.stringify(definition);
  }
  
  /**
   * 后端 definitionJson → 树结构
   */
  static definitionJsonToState(json: string): {
    tree: ApprovalFlowTree;
    meta?: ApprovalDefinitionMeta;
    lfForm?: LfFormPayload;
  } {
    if (!json || json === '{}') {
      return { tree: this.createDefaultTree() };
    }

    try {
      const parsed = JSON.parse(json);
      if (parsed?.nodes?.rootNode) {
        const tree: ApprovalFlowTree = {
          flowName: parsed.meta?.flowName ?? '',
          rootNode: this.definitionNodeToTree(parsed.nodes.rootNode)
        };
        return {
          tree,
          meta: parsed.meta,
          lfForm: parsed.lfForm
        };
      }

      if (parsed?.nodes && parsed?.edges) {
        const tree = this.graphToTree(parsed.nodes || [], parsed.edges || []);
        return { tree };
      }
    } catch (e) {
      console.error('Failed to parse definition json', e);
    }

    return { tree: this.createDefaultTree() };
  }

  static definitionJsonToTree(json: string): ApprovalFlowTree {
    return this.definitionJsonToState(json).tree;
  }
  
  // 私有辅助方法：遍历树生成图
  private static traverseTree(
    node: TreeNode | undefined,
    parentId: string | null,
    nodes: any[],
    edges: any[],
    position: { x: number, y: number }
  ): string | null {
    if (!node) return null;

    // 添加节点
    const nodeData = {
        id: node.id,
        type: node.nodeType,
        label: node.nodeName,
        x: position.x,
        y: position.y,
        // 特定属性
        assigneeType: (node as ApproveNode).assigneeType,
        assigneeValue: (node as ApproveNode).assigneeValue,
        approvalMode: (node as ApproveNode).approvalMode,
        copyToUsers: (node as CopyNode).copyToUsers,
        // conditionRule 存储在边上，或者如果是条件节点，可能需要特殊处理
        // 在 AntFlow 原有逻辑中，条件节点本身不存 rule，rule 存在从条件节点出去的 edge 上
    };
    
    // 规范化数据，确保 X6 可以读取
    nodes.push({
      id: node.id,
      shape: 'custom-node', // 假设使用自定义节点 shape
      x: position.x,
      y: position.y,
      label: node.nodeName,
      data: nodeData,
      type: node.nodeType // 兼容旧逻辑，直接放在顶层属性
    });

    // 如果有父节点，建立连线
    if (parentId) {
       // 注意：如果是条件分支，连线可能需要携带条件规则
       // 这里 traverseTree 主要是处理主干。条件分支在下面单独处理。
       // 这里的 parentId 是指直接的前驱节点
       edges.push({
         source: parentId,
         target: node.id,
         data: {} 
       });
    }

    let lastNodeId = node.id;
    let nextY = position.y + 100;

    // 处理条件/动态条件/条件并行节点
    if (node.nodeType === 'condition' || node.nodeType === 'dynamicCondition' || node.nodeType === 'parallelCondition') {
      const conditionNode = node as ConditionNode | DynamicConditionNode | ParallelConditionNode;
      // 条件节点本身是一个分流点。
      // 它的 conditionNodes (branches) 是并行的。
      // 每个 branch 的第一个节点连接自 conditionNode。
      
      // 收集所有分支的汇聚点（如果有）
      // 在树形结构中，分支最终会汇聚到 conditionNode.childNode (如果有)
      // 或者分支各自结束。
      
      // 我们需要递归处理每个分支
      const branchCount = conditionNode.conditionNodes.length;
      let startX = position.x - (branchCount - 1) * 100; // 简单排版

      const branchEndIds: string[] = [];

      conditionNode.conditionNodes.forEach((branch, index) => {
         // 分支的第一个节点（如果有）
         // 实际上 branch.childNode 是分支的开始节点
         if (branch.childNode) {
            // 建立从 conditionNode 到 branch.childNode 的连线
            // 这条连线需要携带条件
            const branchFirstNodeId = branch.childNode.id;
            
            // 递归处理分支子树
            // 分支的 parentId 是 conditionNode.id
            // 但是我们需要在 edge 上加条件，所以不能直接用 traverseTree 的默认连线逻辑
            // 我们手动调用 traverseTree 但不传 parentId，然后手动加 edge
            
            const branchLastId = this.traverseTree(branch.childNode, null, nodes, edges, { x: startX + index * 200, y: nextY });
            
            if (branchLastId) {
                branchEndIds.push(branchLastId);
            }

            // 添加带条件的连线
            edges.push({
                source: conditionNode.id,
                target: branchFirstNodeId,
                data: {
                    conditionRule: branch.conditionRule
                },
                label: branch.branchName // 可选：在线上显示分支名
            });
         } else {
             // 空分支？直接连接到汇聚点？
             // 如果分支为空，意味着直接通过。
             branchEndIds.push(conditionNode.id); 
             // 这种情况下，如果 conditionNode 后面有 childNode，
             // 那么应该有一条线从 conditionNode 直接连到 childNode，且带有该分支的条件。
             // 但这样会导致多条线连到同一个 childNode。
             // 更好的做法是：空分支不生成中间节点，直接连到汇聚点。
             // 待会儿处理汇聚时，如果 branchEndId 是 conditionNode.id，说明是空分支。
         }
      });

      // 处理汇聚
      if (conditionNode.childNode) {
          // 递归生成汇聚后的节点
          // 它的位置在所有分支下面
          const mergeY = nextY + 200; // 简化计算
          const childLastId = this.traverseTree(conditionNode.childNode, null, nodes, edges, { x: position.x, y: mergeY });
          
          // 将所有分支的末端连接到 childNode
          const mergeTargetId = conditionNode.childNode.id;
          
          branchEndIds.forEach((endId, index) => {
              const branch = conditionNode.conditionNodes[index];
              
              if (endId === conditionNode.id) {
                  // 空分支，直接从 conditionNode 连到 mergeTargetId
                  edges.push({
                      source: conditionNode.id,
                      target: mergeTargetId,
                      data: {
                          conditionRule: branch.conditionRule
                      },
                      label: branch.branchName
                  });
              } else {
                  // 分支末端连到 mergeTargetId
                  edges.push({
                      source: endId,
                      target: mergeTargetId,
                      data: {}
                  });
              }
          });
          
          if (childLastId) lastNodeId = childLastId;
      } else {
          // 没有汇聚节点，分支各自结束
          // 如果分支末端不是 EndNode，可能需要补 EndNode？
          // 在树结构定义中，EndNode 是明确的节点。
          // 如果分支里有 EndNode，traverseTree 会处理。
      }

      return lastNodeId;
    }

    // 处理并行审批节点
    if (node.nodeType === 'parallel') {
      const parallelNode = node as ParallelNode;
      const branchCount = parallelNode.parallelNodes.length;
      let startX = position.x - (branchCount - 1) * 100;

      const branchEndIds: string[] = [];
      parallelNode.parallelNodes.forEach((branch, index) => {
        const branchLastId = this.traverseTree(branch, null, nodes, edges, { x: startX + index * 200, y: nextY });
        if (branchLastId) {
          branchEndIds.push(branchLastId);
        }
        edges.push({
          source: parallelNode.id,
          target: branch.id,
          data: {},
          label: branch.nodeName
        });
      });

      if (parallelNode.childNode) {
        const mergeY = nextY + 200;
        const childLastId = this.traverseTree(parallelNode.childNode, null, nodes, edges, { x: position.x, y: mergeY });
        const mergeTargetId = parallelNode.childNode.id;
        branchEndIds.forEach((endId) => {
          edges.push({
            source: endId,
            target: mergeTargetId,
            data: {}
          });
        });
        if (childLastId) lastNodeId = childLastId;
      }

      return lastNodeId;
    }

    // 处理普通节点的后续
    if ('childNode' in node && (node as any).childNode) {
        return this.traverseTree((node as any).childNode, node.id, nodes, edges, { x: position.x, y: nextY });
    }

    return lastNodeId;
  }

  private static treeNodeToDefinition(node: TreeNode): ApprovalNode {
    const base: ApprovalNode = {
      nodeId: node.id,
      nodeType: node.nodeType,
      nodeName: node.nodeName,
      childNode: node.childNode ? this.treeNodeToDefinition(node.childNode) : undefined
    };

    if (node.nodeType === 'approve') {
      const approveNode = node as ApproveNode;
      base.approverConfig = approveNode.approverConfig ?? {
        setType: approveNode.assigneeType ?? 0,
        signType: approveNode.approvalMode === 'sequential' ? 3 : approveNode.approvalMode === 'any' ? 2 : 1,
        noHeaderAction: approveNode.noHeaderAction ?? 0,
        nodeApproveList: approveNode.assigneeValue
          ? [{ targetId: approveNode.assigneeValue, name: approveNode.assigneeValue }]
          : []
      };
      base.buttonPermissionConfig = approveNode.buttonPermissionConfig;
      base.formPermissionConfig = approveNode.formPermissionConfig;
      base.noticeConfig = approveNode.noticeConfig;
    }

    if (node.nodeType === 'copy') {
      const copyNode = node as CopyNode;
      base.copyConfig = {
        nodeApproveList: (copyNode.copyToUsers || []).map((id) => ({
          targetId: id,
          name: id
        }))
      };
      base.formPermissionConfig = copyNode.formPermissionConfig;
    }

    if (node.nodeType === 'condition' || node.nodeType === 'dynamicCondition' || node.nodeType === 'parallelCondition') {
      const conditionNode = node as ConditionNode | DynamicConditionNode | ParallelConditionNode;
      base.conditionNodes = (conditionNode.conditionNodes || []).map((branch) => this.branchToDefinition(branch));
    }

    if (node.nodeType === 'parallel') {
      const parallelNode = node as ParallelNode;
      base.parallelNodes = (parallelNode.parallelNodes || []).map((child) => this.treeNodeToDefinition(child));
    }

    return base;
  }

  private static branchToDefinition(branch: ConditionBranch): DefinitionBranch {
    return {
      id: branch.id,
      branchName: branch.branchName,
      conditionRule: branch.conditionRule,
      isDefault: branch.isDefault,
      childNode: branch.childNode ? this.treeNodeToDefinition(branch.childNode) : undefined
    };
  }

  private static definitionNodeToTree(node: ApprovalNode): TreeNode {
    const base = {
      id: node.nodeId,
      nodeType: node.nodeType,
      nodeName: node.nodeName
    } as TreeNode;

    if (node.nodeType === 'approve') {
      const config = node.approverConfig;
      const assigneeValue = config?.nodeApproveList?.[0]?.targetId ?? '';
      return {
        ...base,
        nodeType: 'approve',
        assigneeType: config?.setType ?? 0,
        assigneeValue,
        approvalMode: config?.signType === 3 ? 'sequential' : config?.signType === 2 ? 'any' : 'all',
        noHeaderAction: config?.noHeaderAction ?? 0,
        buttonPermissionConfig: node.buttonPermissionConfig,
        formPermissionConfig: node.formPermissionConfig,
        noticeConfig: node.noticeConfig,
        childNode: node.childNode ? this.definitionNodeToTree(node.childNode) : undefined
      } as ApproveNode;
    }

    if (node.nodeType === 'copy') {
      return {
        ...base,
        nodeType: 'copy',
        copyToUsers: (node.copyConfig?.nodeApproveList || []).map((item) => item.targetId),
        formPermissionConfig: node.formPermissionConfig,
        childNode: node.childNode ? this.definitionNodeToTree(node.childNode) : undefined
      } as CopyNode;
    }

    if (node.nodeType === 'condition') {
      return {
        ...base,
        nodeType: 'condition',
        conditionNodes: (node.conditionNodes || []).map((branch) => this.definitionBranchToTree(branch)),
        childNode: node.childNode ? this.definitionNodeToTree(node.childNode) : undefined
      } as ConditionNode;
    }

    if (node.nodeType === 'dynamicCondition') {
      return {
        ...base,
        nodeType: 'dynamicCondition',
        conditionNodes: (node.conditionNodes || []).map((branch) => this.definitionBranchToTree(branch)),
        childNode: node.childNode ? this.definitionNodeToTree(node.childNode) : undefined
      } as DynamicConditionNode;
    }

    if (node.nodeType === 'parallelCondition') {
      return {
        ...base,
        nodeType: 'parallelCondition',
        conditionNodes: (node.conditionNodes || []).map((branch) => this.definitionBranchToTree(branch)),
        childNode: node.childNode ? this.definitionNodeToTree(node.childNode) : undefined
      } as ParallelConditionNode;
    }

    if (node.nodeType === 'parallel') {
      return {
        ...base,
        nodeType: 'parallel',
        parallelNodes: (node.parallelNodes || []).map((child) => this.definitionNodeToTree(child)),
        childNode: node.childNode ? this.definitionNodeToTree(node.childNode) : undefined
      } as ParallelNode;
    }

    return {
      ...base,
      childNode: node.childNode ? this.definitionNodeToTree(node.childNode) : undefined
    } as TreeNode;
  }

  private static definitionBranchToTree(branch: DefinitionBranch): ConditionBranch {
    return {
      id: branch.id,
      branchName: branch.branchName,
      conditionRule: branch.conditionRule,
      isDefault: branch.isDefault,
      childNode: branch.childNode ? this.definitionNodeToTree(branch.childNode) : undefined
    };
  }
  
  // 私有辅助方法：从图构建树
  private static buildTreeFromNode(
    nodeId: string,
    allNodes: any[],
    outgoing: Map<string, any[]>
  ): TreeNode {
    const nodeData = allNodes.find(n => n.id === nodeId);
    if (!nodeData) throw new Error(`Node ${nodeId} not found`);

    const type = nodeData.data?.type || nodeData.type;
    const base: any = {
        id: nodeData.id,
        nodeType: type,
        nodeName: nodeData.data?.label || nodeData.label || '未命名',
    };

    // 获取出边
    const edges = outgoing.get(nodeId) || [];

    if (type === 'start') {
        const node: StartNode = { ...base };
        if (edges.length > 0) {
            // 假设 Start 只有一个后续
            node.childNode = this.buildTreeFromNode(edges[0].target.cell || edges[0].target, allNodes, outgoing);
        }
        return node;
    } else if (type === 'approve') {
        const node: ApproveNode = {
            ...base,
            assigneeType: nodeData.data?.assigneeType,
            assigneeValue: nodeData.data?.assigneeValue,
            approvalMode: nodeData.data?.approvalMode
        };
        if (edges.length > 0) {
            // 假设 Approve 只有一个后续（除非接了条件节点，但条件节点也是一个节点）
             node.childNode = this.buildTreeFromNode(edges[0].target.cell || edges[0].target, allNodes, outgoing);
        }
        return node;
    } else if (type === 'copy') {
        const node: CopyNode = {
            ...base,
            copyToUsers: nodeData.data?.copyToUsers || []
        };
        if (edges.length > 0) {
             node.childNode = this.buildTreeFromNode(edges[0].target.cell || edges[0].target, allNodes, outgoing);
        }
        return node;
    } else if (type === 'condition' || type === 'dynamicCondition' || type === 'parallelCondition') {
        // 这里的逻辑比较复杂。
        // 在 X6 图中，Condition 节点可能有多个出边，每个出边代表一个分支。
        // 我们需要识别这些分支，以及它们是否汇聚。
        
        const node: ConditionNode | DynamicConditionNode | ParallelConditionNode = {
            ...base,
            nodeType: type,
            conditionNodes: []
        } as ConditionNode;

        // 收集分支
        // 假设 edges 上的 data.conditionRule 对应分支条件
        const branches: ConditionBranch[] = edges.map((edge, index) => {
            const targetId = edge.target.cell || edge.target;
            const branchRoot = this.buildTreeFromNode(targetId, allNodes, outgoing);
            
            return {
                id: nanoid(),
                branchName: edge.label || `条件${index + 1}`,
                conditionRule: edge.data?.conditionRule,
                childNode: branchRoot
            };
        });
        
        node.conditionNodes = branches;
        
        // 难点：如何识别汇聚点？
        // 在图转树的过程中，如果多个分支最终指向同一个节点，那个节点就是汇聚点。
        // 简单的递归 buildTreeFromNode 会导致汇聚点被重复构建在每个分支的末尾。
        // 这是一个图转树的经典问题。
        // 鉴于 AntFlow 的树结构设计，我们可能需要一种策略来“剪断”汇聚点，将其提升为 conditionNode.childNode。
        
        // 简化策略：
        // 1. 遍历所有分支，看它们的子孙节点是否有相同的 ID。
        // 2. 如果有，提取出来作为 conditionNode.childNode。
        
        // 由于时间限制，且通常设计器生成的图结构比较规范。
        // 我们假设：如果所有分支的路径最终都汇聚到同一个节点 X，则 X 是 conditionNode.childNode。
        // 这里暂时不实现复杂的图算法，而是假设 graphToTree 主要用于从简单的 definitionJson 恢复。
        // 如果是从复杂的图（允许任意连线）恢复，可能无法完美映射到树。
        
        return node as TreeNode;
    } else if (type === 'parallel') {
        const node: ParallelNode = {
            ...base,
            nodeType: 'parallel',
            parallelNodes: edges.map((edge) => {
                const targetId = edge.target.cell || edge.target;
                return this.buildTreeFromNode(targetId, allNodes, outgoing);
            })
        };
        return node;
    } else if (type === 'end') {
        return { ...base } as EndNode;
    }

    return base as TreeNode;
  }
}
