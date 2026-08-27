<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { ClusterConfig, RouteConfig } from '@/types/controlPlane'

const props = defineProps<{ route: RouteConfig; clusters: ClusterConfig[]; readonly: boolean }>()
const { t } = useI18n()

const methods = computed({
  get: () => props.route.match.methods?.join(', ') ?? '',
  set: value => {
    if (props.readonly) return
    const parsed = value.split(',').map(item => item.trim().toUpperCase()).filter(Boolean)
    if (parsed.length) props.route.match.methods = parsed
    else delete props.route.match.methods
  }
})

const removePrefix = computed({
  get: () => props.route.transforms?.find(transform => 'PathRemovePrefix' in transform)?.PathRemovePrefix ?? '',
  set: value => {
    if (props.readonly) return
    const transforms = [...(props.route.transforms ?? [])]
    const index = transforms.findIndex(transform => 'PathRemovePrefix' in transform)
    if (value) {
      const next = { PathRemovePrefix: value }
      if (index >= 0) transforms[index] = next
      else transforms.push(next)
    } else if (index >= 0) {
      transforms.splice(index, 1)
    }
    if (transforms.length) props.route.transforms = transforms
    else delete props.route.transforms
  }
})
</script>

<template>
  <div class="resource-editor">
    <div class="editor-section">
      <div class="editor-section-title"><span>{{ t('editor.route.identity') }}</span><small>{{ t('editor.route.tag') }}</small></div>
      <div class="form-grid">
        <label class="field field-wide">
          <span>{{ t('editor.route.routeId') }}</span>
          <a-input v-model="route.routeId" :disabled="readonly" placeholder="orders-route" />
        </label>
        <label class="field">
          <span>{{ t('editor.route.cluster') }}</span>
          <a-select v-model="route.clusterId" allow-clear :disabled="readonly" :placeholder="t('editor.route.selectCluster')">
            <a-option v-for="cluster in clusters" :key="cluster.clusterId" :value="cluster.clusterId">{{ cluster.clusterId }}</a-option>
          </a-select>
        </label>
        <label class="field">
          <span>{{ t('editor.route.order') }}</span>
          <a-input-number v-model="route.order" :disabled="readonly" placeholder="0" />
        </label>
      </div>
    </div>

    <div class="editor-section">
      <div class="editor-section-title"><span>{{ t('editor.route.match') }}</span><small>{{ t('editor.route.inbound') }}</small></div>
      <div class="form-grid">
        <label class="field field-wide">
          <span>{{ t('editor.route.pathPattern') }}</span>
          <a-input v-model="route.match.path" :disabled="readonly" placeholder="/orders/{**catch-all}" />
        </label>
        <label class="field field-wide">
          <span>{{ t('editor.route.methods') }}</span>
          <a-input v-model="methods" :disabled="readonly" placeholder="GET, POST" />
          <small>{{ t('editor.route.methodsHint') }}</small>
        </label>
      </div>
    </div>

    <div class="editor-section">
      <div class="editor-section-title"><span>{{ t('editor.route.transform') }}</span><small>{{ t('editor.route.outbound') }}</small></div>
      <label class="field">
        <span>{{ t('editor.route.removePrefix') }}</span>
        <a-input v-model="removePrefix" :disabled="readonly" placeholder="/orders" />
      </label>
    </div>
  </div>
</template>

<style scoped>
.resource-editor { display: grid; gap: 28px; }
.editor-section { display: grid; gap: 15px; }
.editor-section-title { display: flex; padding-bottom: 9px; border-bottom: 1px solid var(--line); align-items: center; justify-content: space-between; }
.editor-section-title span { font: 620 14px/1 'Space Grotesk Variable', sans-serif; }
.editor-section-title small { color: var(--muted); font: 400 9px/1 'IBM Plex Mono', monospace; }
.form-grid { display: grid; grid-template-columns: 1fr 160px; gap: 16px; }
.field { display: grid; gap: 7px; color: var(--ink-soft); font-size: 11px; font-weight: 650; }
.field > small { color: var(--muted); font-size: 10px; font-weight: 400; }
.field-wide { grid-column: 1 / -1; }
@media (max-width: 620px) { .form-grid { grid-template-columns: 1fr; } .field-wide { grid-column: auto; } }
</style>