import { createApp } from 'vue'
import '@fontsource-variable/manrope'
import '@fontsource-variable/space-grotesk'
import '@fontsource/ibm-plex-mono/400.css'
import App from './App.vue'
import { i18n } from './i18n'
import router from './router'
import { pinia } from './stores'
import './styles/global.css'

createApp(App).use(pinia).use(i18n).use(router).mount('#app')