<script setup lang="ts">
import { useI18n } from 'vue-i18n'

defineProps<{ compact?: boolean; inverse?: boolean }>()

const { t } = useI18n()
</script>

<template>
  <div class="brand" :class="{ compact, inverse }" :aria-label="t('brand.name')">
    <span class="brand-mark" aria-hidden="true">
      <i class="mark-node mark-node-a" />
      <i class="mark-line" />
      <i class="mark-node mark-node-b" />
    </span>
    <span v-if="!compact" class="brand-copy">
      <strong>{{ t('brand.name') }}</strong>
      <small>{{ t('brand.subtitle') }}</small>
    </span>
  </div>
</template>

<style scoped>
.brand {
  display: inline-flex;
  align-items: center;
  gap: 11px;
}

.brand-mark {
  position: relative;
  display: block;
  width: 32px;
  height: 32px;
  flex: 0 0 32px;
  border: 1px solid var(--line-strong);
  border-radius: 4px;
}

.mark-node {
  position: absolute;
  z-index: 1;
  width: 6px;
  height: 6px;
  background: var(--blue);
  border-radius: 50%;
}

.mark-node-a { top: 7px; left: 7px; }
.mark-node-b { right: 7px; bottom: 7px; background: var(--green); }

.mark-line {
  position: absolute;
  top: 15px;
  left: 10px;
  width: 14px;
  height: 1px;
  background: var(--ink);
  transform: rotate(45deg);
}

.brand-copy {
  display: grid;
  gap: 1px;
}

.brand strong {
  font: 650 16px/1.1 'Space Grotesk Variable', sans-serif;
}

.brand small {
  color: var(--muted);
  font: 400 9px/1.2 'IBM Plex Mono', monospace;
  text-transform: uppercase;
}

.inverse .brand-mark { border-color: rgb(255 255 255 / 30%); }
.inverse .mark-line { background: #fff; }
.inverse strong { color: #fff; }
.inverse small { color: #98aaa3; }
</style>