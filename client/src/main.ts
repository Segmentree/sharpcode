import { createApp } from 'vue'
import App from './App.vue'
import { router } from './router'
import { i18n } from './i18n'
import './style.css'

// Per-problem drafts were replaced by "latest submission or starter code"; clear leftovers from older versions.
try {
  Object.keys(localStorage)
    .filter((k) => k.startsWith('sharpcode:draft:'))
    .forEach((k) => localStorage.removeItem(k))
} catch {
  /* storage unavailable */
}

createApp(App).use(router).use(i18n).mount('#app')
