import { computed, ref } from 'vue'

const STORAGE_KEY = 'sharpcode:adminToken'

function readStored(): string {
  try {
    return localStorage.getItem(STORAGE_KEY) ?? ''
  } catch {
    return ''
  }
}

/** Admin token for this browser. Empty string means not signed in. */
export const adminToken = ref<string>(readStored())
export const isAdmin = computed(() => adminToken.value.length > 0)

export function setAdminToken(token: string) {
  adminToken.value = token
  try {
    if (token) localStorage.setItem(STORAGE_KEY, token)
    else localStorage.removeItem(STORAGE_KEY)
  } catch {
    /* ignore */
  }
}

export function clearAdminToken() {
  setAdminToken('')
}
