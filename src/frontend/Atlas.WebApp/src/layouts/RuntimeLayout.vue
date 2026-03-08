<template>
  <div class="runtime-layout">
    <header class="runtime-header">
      <div class="runtime-title">Runtime Delivery</div>
    </header>
    <main class="runtime-content">
      <RouterView />
    </main>
  </div>
</template>

<style scoped>
.runtime-layout {
  min-height: 100vh;
  background: #f5f7fb;
}

.runtime-header {
  height: 56px;
  display: flex;
  align-items: center;
  padding: 0 16px;
  background: #fff;
  border-bottom: 1px solid #f0f0f0;
}

.runtime-title {
  font-size: 16px;
  font-weight: 600;
}

.runtime-content {
  padding: 16px;
}
</style>
