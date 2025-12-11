import type { RefreshTokenResponse } from "./types"

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL 

const STORAGE_KEYS = {
    accessToken: "auth_access_token",
    refreshToken: "auth_refresh_token",
    expiresAt: "auth_expires_at",
    authUser: "authUser",
}

let refreshTimeoutId: ReturnType<typeof setTimeout> | null = null
let onTokenRefreshFailed: (() => void) | null = null

export function setTokenRefreshFailedCallback(callback: () => void) {
    onTokenRefreshFailed = callback
}

export function setTokens(accessToken: string, refreshToken: string, expiresAt: string) {
    if (typeof window === "undefined") return
    
    sessionStorage.setItem(STORAGE_KEYS.accessToken, accessToken)
    sessionStorage.setItem(STORAGE_KEYS.refreshToken, refreshToken)
    sessionStorage.setItem(STORAGE_KEYS.expiresAt, expiresAt)
    
    scheduleTokenRefresh()
}

export function getAccessToken(): string | null {
    if (typeof window === "undefined") return null
   
    return sessionStorage.getItem(STORAGE_KEYS.accessToken)
}

export function getRefreshToken(): string | null {
    if (typeof window === "undefined") return null
   
    return sessionStorage.getItem(STORAGE_KEYS.refreshToken)
}

export function getStoredUser() {
    if (typeof window === "undefined") return null
    
    const userStr = sessionStorage.getItem(STORAGE_KEYS.authUser)
    if (!userStr) return null
    try {
        return JSON.parse(userStr)
    } catch {
       
        sessionStorage.removeItem(STORAGE_KEYS.authUser)
        return null
    }
}

export function setStoredUser(user: any) {
    if (typeof window === "undefined") return
  
    sessionStorage.setItem(STORAGE_KEYS.authUser, JSON.stringify(user))
}

export function clearTokens() {
    if (typeof window !== "undefined") {
      
        sessionStorage.removeItem(STORAGE_KEYS.accessToken)
        sessionStorage.removeItem(STORAGE_KEYS.refreshToken)
        sessionStorage.removeItem(STORAGE_KEYS.expiresAt)
        sessionStorage.removeItem(STORAGE_KEYS.authUser)
    }
    if (refreshTimeoutId) {
        clearTimeout(refreshTimeoutId)
        refreshTimeoutId = null
    }
}

export function hasValidTokens(): boolean {
    if (typeof window === "undefined") return false
    const accessToken = getAccessToken()
   
    const expiresAt = sessionStorage.getItem(STORAGE_KEYS.expiresAt)
    if (!accessToken || !expiresAt) return false
    return new Date(new Date(expiresAt).getTime() - 30000) > new Date()
}


function scheduleTokenRefresh() {
    if (refreshTimeoutId) clearTimeout(refreshTimeoutId)
    if (typeof window === "undefined") return

   
    const expiresAtStr = sessionStorage.getItem(STORAGE_KEYS.expiresAt)
    if (!expiresAtStr) return

    const expiresAt = new Date(expiresAtStr)
    const now = new Date()
    const refreshTime = expiresAt.getTime() - now.getTime() - 60000

    if (refreshTime <= 0) {
        refreshAccessToken()
        return
    }

    refreshTimeoutId = setTimeout(refreshAccessToken, refreshTime)
}

async function refreshAccessToken(): Promise<boolean> {
    const accessToken = getAccessToken()
    const refreshToken = getRefreshToken()
    const currentUser = getStoredUser()
    if (!accessToken || !refreshToken || !currentUser) return false

    try {
        const response = await fetch(`${API_BASE_URL}/Auth/refresh`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ accessToken, refreshToken }),
        })

        const data: RefreshTokenResponse = await response.json()

        if (data.success && data.data) {
           
            if (data.data.userId !== currentUser.userId) return false

            setTokens(data.data.token, data.data.refreshToken, data.data.expiresAt)
            setStoredUser({ ...currentUser, role: data.data.role })
            return true
        }

        clearTokens()
        onTokenRefreshFailed?.()
        return false
    } catch {
        clearTokens()
        onTokenRefreshFailed?.()
        return false
    }
}


export async function tryRefreshToken(): Promise<boolean> {
    return refreshAccessToken()
}

export async function refreshAndGetNewTokenData(): Promise<{
    token: string
    refreshToken: string
    expiresAt: string
    userId: string
    userName: string
    email: string
    firstName: string
    lastName: string
    fullName: string
    role: string
} | null> {
    const accessToken = getAccessToken()
    const refreshToken = getRefreshToken()
    const currentUser = getStoredUser()
    if (!accessToken || !refreshToken || !currentUser) return null

    try {
        const response = await fetch(`${API_BASE_URL}/Auth/refresh`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ accessToken, refreshToken }),
        })

        const data: RefreshTokenResponse = await response.json()

        if (data.success && data.data && data.data.userId === currentUser.userId) {
            setTokens(data.data.token, data.data.refreshToken, data.data.expiresAt)
            setStoredUser({ ...currentUser, role: data.data.role })
            return { ...data.data }
        }

        return null
    } catch {
        return null
    }
}


if (typeof window !== "undefined" && hasValidTokens()) {
    scheduleTokenRefresh()
}