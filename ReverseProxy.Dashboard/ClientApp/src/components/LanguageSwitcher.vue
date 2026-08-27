<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { Languages } from '@lucide/vue'
import { setLocale, type AppLocale } from '@/i18n'

defineProps<{ inverse?: boolean }>()

const { locale, t } = useI18n()

const options: Array<{ value: AppLocale; key: 'chinese' | 'english' }> = [
  { value: 'zh-CN', key: 'chinese' },
  { value: 'en-US', key: 'english' }
]
</script>

<template>
  <div class="language-switcher" :class="{ inverse }" role="group" :aria-label="t('language.label')">
    <Languages :size="15" :stroke-width="1.8" aria-hidden="true" />
    <div class="language-options">
      <button
        v-for="option in options"
        :key="option.value"
        type="button"
        :class="{ active: locale === option.value }"
        :aria-pressed="locale === option.value"
        @click="setLocale(option.value)"
      >
        {{ t(`language.${option.key}`) }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.language-switcher {
  display: inline-flex;
  height: 34px;
  padding: 3px 3px 3px 8px;
  color: var(--muted);
  background: rgb(255 255 255 / 72%);
  border: 1px solid var(--line);
  border-radius: 4px;
  align-items: center;
  gap: 6px;
}

.language-options { display: grid; grid-template-columns: repeat(2, minmax(38px, auto)); gap: 2px; }
.language-options button { height: 26px; padding: 0 8px; color: inherit; background: transparent; border: 0; border-radius: 3px; cursor: pointer; font-size: 10px; font-weight: 650; }
.language-options button:hover { color: var(--ink); }
.language-options button.active { color: var(--ink); background: var(--surface); box-shadow: 0 1px 3px rgb(23 35 31 / 12%); }

.inverse { color: #91a39c; background: rgb(255 255 255 / 5%); border-color: rgb(255 255 255 / 12%); }
.inverse .language-options button:hover { color: #fff; }
.inverse .language-options button.active { color: #fff; background: #30443d; box-shadow: none; }
</style>