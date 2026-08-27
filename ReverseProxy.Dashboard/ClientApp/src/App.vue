<script setup lang="ts">
import { watchEffect } from 'vue'
import { RouterView, useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'

const route = useRoute()
const { t } = useI18n()

watchEffect(() => {
  const titleKey = typeof route.meta.titleKey === 'string' ? route.meta.titleKey : 'pageTitle.console'
  document.title = `${t(titleKey)} · Signal Plane`
})
</script>

<template>
  <RouterView />
</template>
