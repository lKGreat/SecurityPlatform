/**
 * 插件前端资源加载器
 * 从后端加载插件的前端 JS bundle，执行注册逻辑
 */

const _loadedBundles = new Set<string>()

/**
 * 加载一个插件的前端 bundle
 * bundle 应暴露全局 window.__AtlasPlugin_<code> 对象并自行调用 registry 注册函数
 */
export async function loadPluginBundle(pluginCode: string, bundleUrl: string): Promise<void> {
  const key = `${pluginCode}@${bundleUrl}`
  if (_loadedBundles.has(key)) {
    return
  }

  await new Promise<void>((resolve, reject) => {
    const script = document.createElement('script')
    script.src = bundleUrl
    script.async = true
    script.onload = () => {
      _loadedBundles.add(key)
      resolve()
    }
    script.onerror = () => reject(new Error(`Failed to load plugin bundle: ${bundleUrl}`))
    document.head.appendChild(script)
  })
}

/**
 * 卸载一个插件的 bundle（仅从已加载集合移除，无法真正卸载已执行的 JS）
 */
export function unloadPluginBundle(pluginCode: string): void {
  for (const key of _loadedBundles) {
    if (key.startsWith(`${pluginCode}@`)) {
      _loadedBundles.delete(key)
    }
  }
}

export function isPluginBundleLoaded(pluginCode: string): boolean {
  return [..._loadedBundles].some((k) => k.startsWith(`${pluginCode}@`))
}
