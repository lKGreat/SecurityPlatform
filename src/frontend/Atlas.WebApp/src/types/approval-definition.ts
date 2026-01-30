export type ApprovalNodeType =
  | 'start'
  | 'approve'
  | 'copy'
  | 'condition'
  | 'parallel'
  | 'dynamicCondition'
  | 'parallelCondition'
  | 'end';

export interface VisibilityScope {
  scopeType: 'All' | 'Department' | 'Role' | 'User';
  departmentIds?: number[];
  roleCodes?: string[];
  userIds?: number[];
}

export interface ApprovalDefinitionMeta {
  flowName: string;
  description?: string;
  category?: string;
  visibilityScope?: VisibilityScope;
  isQuickEntry?: boolean;
  isLowCodeFlow?: boolean;
}

export interface LfFormField {
  fieldId: string;
  fieldName: string;
  fieldType: string;
  valueType: string;
  options?: Array<{ key: string; value: string }>;
}

export interface LfFormPayload {
  formJson: unknown;
  formFields: LfFormField[];
}

export interface ButtonPermissionConfig {
  startPage?: number[];
  approvalPage?: number[];
  viewPage?: number[];
}

export interface FormPermissionConfig {
  fields: Array<{ fieldId: string; perm: 'R' | 'E' | 'H' }>;
}

export interface NoticeConfig {
  channelIds: number[];
  templateId?: string;
}

export interface ApproverConfig {
  setType: number;
  signType: number;
  noHeaderAction: number;
  nodeApproveList: Array<{ targetId: string; name: string }>;
}

export interface CopyConfig {
  nodeApproveList: Array<{ targetId: string; name: string }>;
}

export interface ConditionItem {
  fieldId: string;
  fieldType: string;
  operator: string;
  value: string | number | boolean | string[];
}

export interface ConditionGroup {
  condRelation: boolean;
  items: ConditionItem[];
}

export interface ConditionConfig {
  isDefault?: boolean;
  groupRelation?: boolean;
  conditionGroups: ConditionGroup[];
}

export interface ParallelConfig {
  parallelNodes: ApprovalNode[];
}

export interface ApprovalNode {
  nodeId: string;
  nodeType: ApprovalNodeType;
  nodeName: string;
  childNode?: ApprovalNode;
  conditionNodes?: ConditionBranch[];
  parallelNodes?: ApprovalNode[];
  approverConfig?: ApproverConfig;
  copyConfig?: CopyConfig;
  conditionConfig?: ConditionConfig;
  parallelConfig?: ParallelConfig;
  noticeConfig?: NoticeConfig;
  formPermissionConfig?: FormPermissionConfig;
  buttonPermissionConfig?: ButtonPermissionConfig;
}

export interface ConditionRule {
  field: string;
  operator: string;
  value: string | number | boolean | string[];
}

export interface ConditionBranch {
  id: string;
  branchName: string;
  conditionRule?: ConditionRule;
  childNode?: ApprovalNode;
  isDefault?: boolean;
}

export interface ApprovalDefinitionJson {
  meta: ApprovalDefinitionMeta;
  lfForm?: LfFormPayload;
  nodes: { rootNode: ApprovalNode };
}
