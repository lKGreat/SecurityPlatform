import type { AmisEnv, AmisSchema } from "@/types/amis";
import type { JsonValue } from "@/types/api";

declare module "amis" {
  export interface AmisRenderOptions {
    data?: JsonValue;
    env?: AmisEnv;
    locale?: string;
    theme?: string;
  }

  export function render(schema: AmisSchema, options?: AmisRenderOptions, container?: Element): void;
}
