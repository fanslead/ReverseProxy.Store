<script setup lang="ts">
import type { Component } from 'vue'
import { useI18n } from 'vue-i18n'
import { Cloud, GitBranch, RadioTower, Server } from '@lucide/vue'

defineProps<{
  revision?: number
  routeCount?: number
  clusterCount?: number
  loading?: boolean
}>()
const { t } = useI18n()

interface SignalNode {
  key: string
  labelKey: string
  metric?: 'routeCount' | 'clusterCount'
  valueKey?: string
  icon: Component
}

const nodes: SignalNode[] = [
  { key: 'edge', labelKey: 'trace.edgeIntake', valueKey: 'HTTPS', icon: RadioTower },
  { key: 'route', labelKey: 'trace.routeMatcher', metric: 'routeCount', icon: GitBranch },
  { key: 'cluster', labelKey: 'trace.clusterPool', metric: 'clusterCount', icon: Server },
  { key: 'origin', labelKey: 'trace.upstream', valueKey: 'trace.ready', icon: Cloud }
]
</script>

<template>
  <section class="signal-trace panel" :aria-label="t('trace.ariaLabel')">
    <header class="trace-header">
      <div>
        <p class="eyebrow">{{ t('trace.livePath') }}</p>
        <h2 class="display-type">{{ t('trace.requestSignal') }}</h2>
      </div>
      <span class="revision mono">{{ t('trace.revision') }} {{ loading ? '—' : revision }}</span>
    </header>

    <div class="trace-track">
      <div class="track-line"><i class="signal" /></div>
      <div v-for="node in nodes" :key="node.key" class="trace-node">
        <span class="node-icon"><component :is="node.icon" :size="19" :stroke-width="1.8" /></span>
        <strong>{{ t(node.labelKey) }}</strong>
        <small class="mono">
          <template v-if="node.metric === 'routeCount'">{{ loading ? '—' : t('trace.routes', { count: routeCount ?? 0 }, routeCount ?? 0) }}</template>
          <template v-else-if="node.metric === 'clusterCount'">{{ loading ? '—' : t('trace.clusters', { count: clusterCount ?? 0 }, clusterCount ?? 0) }}</template>
          <template v-else>{{ node.valueKey === 'HTTPS' ? 'HTTPS' : t(node.valueKey ?? 'common.unknown') }}</template>
        </small>
      </div>
    </div>
  </section>
</template>

<style scoped>
.signal-trace { padding: 24px 26px 28px; overflow: hidden; }
.trace-header { display: flex; align-items: flex-start; justify-content: space-between; }
.trace-header h2 { margin: 0; font-size: 20px; }
.revision { padding: 6px 8px; color: var(--green); background: var(--green-soft); border-radius: 3px; font-size: 10px; }
.trace-track { position: relative; display: grid; margin-top: 34px; grid-template-columns: repeat(4, 1fr); }
.track-line { position: absolute; top: 18px; right: 10%; left: 10%; height: 2px; overflow: hidden; background: var(--line); }
.signal { position: absolute; top: -1px; left: 0; width: 54px; height: 4px; background: var(--blue); box-shadow: 0 0 16px rgb(36 88 230 / 40%); animation: signal-run 3.4s ease-in-out infinite; }
.trace-node { position: relative; z-index: 1; display: grid; justify-items: center; text-align: center; }
.node-icon { display: grid; width: 38px; height: 38px; color: var(--ink); background: var(--surface); border: 1px solid var(--line-strong); border-radius: 50%; box-shadow: 0 0 0 7px var(--surface); place-items: center; }
.trace-node strong { margin-top: 14px; font-size: 12px; }
.trace-node small { margin-top: 4px; color: var(--muted); font-size: 9px; }
@keyframes signal-run { 0% { transform: translateX(-60px); opacity: 0; } 12%, 82% { opacity: 1; } 100% { transform: translateX(calc(80vw - 80px)); opacity: 0; } }

@media (max-width: 620px) {
  .signal-trace { padding: 20px 18px; }
  .trace-track { margin-top: 24px; grid-template-columns: 1fr; gap: 0; }
  .track-line { top: 20px; bottom: 20px; left: 18px; width: 2px; height: auto; }
  .signal { top: 0; left: -1px; width: 4px; height: 44px; animation-name: signal-run-mobile; }
  .trace-node { min-height: 72px; padding-left: 56px; text-align: left; justify-items: start; align-content: center; }
  .node-icon { position: absolute; top: 1px; left: 0; box-shadow: 0 0 0 6px var(--surface); }
  .trace-node strong { margin-top: 0; }
  @keyframes signal-run-mobile { 0% { transform: translateY(-50px); opacity: 0; } 12%, 82% { opacity: 1; } 100% { transform: translateY(245px); opacity: 0; } }
}
</style>