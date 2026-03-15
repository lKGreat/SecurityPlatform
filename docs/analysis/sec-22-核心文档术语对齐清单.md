# SEC-22 核心文档术语对齐清单

> 任务：`SEC-22` 对齐核心文档主术语与边界描述  
> 日期：2026-03-16  
> 术语基线：`docs/analysis/unified-terminology-glossary-v1.md`

## 一、已完成更新清单

1. `docs/架构与产品能力总览.md`
   - 新增“主术语基线（SEC-22）”章节。
   - 将 `Application / Project / Workflow / DataSource` 的裸词改为受控术语表达。
   - 明确 `WorkflowDefinition` 与 `RuntimeExecution` 的定义态/运行态边界。

2. `docs/多租户多应用.md`
   - 重写为结构化版本，统一对象为 `Tenant / ApplicationCatalog / TenantApplication / TenantAppInstance / TenantDataSource / ProjectAsset`。
   - 删除历史草案式口语描述，补齐关系约束、接口行为约束与验收标准。

3. `docs/plan-平台控制台与应用数据源.md`
   - 新增“术语对齐声明（SEC-22）”。
   - 统一文中对象语义为 `ApplicationCatalog / TenantApplication / TenantAppInstance / TenantDataSource / ProjectAsset`。
   - 在变更记录中补充 SEC-22 对齐记录。

4. `docs/coze-studio-feature-atlas.md`
   - 新增 Coze 语境到平台主术语的映射声明。
   - 将项目与工作流关键域标题改为对齐命名（`ProjectAsset`、`WorkflowDefinition/RuntimeExecution`）。

## 二、当前统一边界（供评审引用）

- 平台目录定义：`ApplicationCatalog`
- 租户开通关系：`TenantApplication`
- 租户应用运行载体：`TenantAppInstance`
- 租户数据连接资源：`TenantDataSource`
- 创作空间：`Workspace`
- 创作资产容器：`ProjectAsset`
- 流程定义态/运行态：`WorkflowDefinition` / `RuntimeExecution`

## 三、待后续文档跟进列表

1. `docs/coze-studio-feature-inventory.md`
   - 原因：清单规模大（大量 API 与模块说明），需专项批量替换并复核上下文语义。

2. `docs/coze-studio-project-cognitive-map.md`
   - 原因：包含大量“项目结构原文映射”，需在“忠实源码目录”与“统一术语”之间采用双栏表达，避免失真。

3. `docs/coze-studio-api-inventory.md`
   - 原因：API 路径需保持原样，术语只能在“解释层”替换，需补充“原始接口名 vs 目标语义”映射表。

4. `docs/coze-studio-tech-stack-analysis.md`
   - 原因：当前以技术栈为主，需补充“对象边界章节”后再对齐主术语，不宜直接全文替换。

5. `docs/项目能力概览.md`
   - 原因：作为产品对外总览，需与 `架构与产品能力总览.md` 一并做术语联动修订。

## 四、一致性检查结论

- 本次优先文档已消除核心冲突对象的裸词主语义竞争（Application、DataSource、Workflow、Project）。
- 后续评审可优先以本清单及已更新文档作为 P0 基线；待跟进文档按本清单第三节逐步完成。
