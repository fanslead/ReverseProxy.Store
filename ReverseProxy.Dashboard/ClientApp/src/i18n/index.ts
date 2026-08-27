import { createI18n } from 'vue-i18n'
import { addI18nMessages, useLocale as useArcoLocale } from '@arco-design/web-vue/es/locale'
import enUS from '@arco-design/web-vue/es/locale/lang/en-us'
import { messages } from './messages'

export type AppLocale = keyof typeof messages

const localeStorageKey = 'signal-plane.locale'
const defaultLocale: AppLocale = 'zh-CN'

function isAppLocale(value: string | null): value is AppLocale {
  return value === 'zh-CN' || value === 'en-US'
}

function getInitialLocale(): AppLocale {
  const storedLocale = localStorage.getItem(localeStorageKey)
  return isAppLocale(storedLocale) ? storedLocale : defaultLocale
}

const initialLocale = getInitialLocale()

addI18nMessages({ 'en-US': enUS })
useArcoLocale(initialLocale)
document.documentElement.lang = initialLocale

export const i18n = createI18n({
  legacy: false,
  locale: initialLocale,
  fallbackLocale: defaultLocale,
  messages
})

export function setLocale(locale: AppLocale) {
  i18n.global.locale.value = locale
  localStorage.setItem(localeStorageKey, locale)
  document.documentElement.lang = locale
  useArcoLocale(locale)
}