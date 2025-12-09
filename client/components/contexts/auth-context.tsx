"use client"

import { createContext, useContext, useEffect, useState, useCallback, type ReactNode } from "react"
import { useRouter } from "next/navigation"
import type { User, AuthResponse } from "@/lib/types"
import {
  setTokens,
  getAccessToken,
  clearTokens,
  setTokenRefreshFailedCallback,
  hasValidTokens,
  tryRefreshToken,
} from "@/lib/token-manager"

interface AuthContextType {
  user: User | null
  token: string | null
  isLoading: boolean
  isAuthenticated: boolean
  login: (authData: AuthResponse["data"]) => void
  logout: () => void
  getValidToken: () => Promise<string | null>
}


const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [token, setToken] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const router = useRouter()

  const logout = useCallback(() => {
    clearTokens()
    if (typeof window !== "undefined") {
      localStorage.removeItem("authUser")
    }
    setToken(null)
    setUser(null)
    router.push("/login")
  }, [router])

  
  useEffect(() => {
    setTokenRefreshFailedCallback(logout)
  }, [logout])

  useEffect(() => {
    const storedUser = typeof window !== "undefined" ? localStorage.getItem("authUser") : null

    if (storedUser) {
      try {
        const parsedUser = JSON.parse(storedUser) as User
        setUser(parsedUser)
      } catch {
        localStorage.removeItem("authUser")
      }
    }

    const initAuth = async () => {
      const currentToken = getAccessToken()

      if (currentToken && hasValidTokens()) {
        setToken(currentToken)
        setIsLoading(false)
        return
      }

      const refreshed = await tryRefreshToken()
      if (refreshed) {
        const newToken = getAccessToken()
        setToken(newToken)
      } else {
        localStorage.removeItem("authUser")
        setUser(null)
      }
      setIsLoading(false)
    }

    void initAuth()
  }, [])

  const login = (authData: AuthResponse["data"]) => {
    const userData: User = {
      userId: authData.userId,
      userName: authData.userName,
      email: authData.email,
      role: authData.role,
      firstName: authData.firstName,
      lastName: authData.lastName,
      fullName: authData.fullName,
    }

    setTokens(authData.token, authData.refreshToken, authData.expiresAt)
    localStorage.setItem("authUser", JSON.stringify(userData))

    setToken(authData.token)
    setUser(userData)

    if (authData.role === "Admin") {
      router.push("/admin")
    } else {
      router.push("/dashboard")
    }
  }

  const getValidToken = useCallback(async (): Promise<string | null> => {
    const currentToken = getAccessToken()

    if (currentToken && hasValidTokens()) {
      return currentToken
    }

    const refreshed = await tryRefreshToken()
    if (refreshed) {
      const newToken = getAccessToken()
      setToken(newToken)
      return newToken
    }

    return null
  }, [])

  const isAuthenticated = user !== null && token !== null

  const value: AuthContextType = {
    user,
    token,
    isLoading,
    isAuthenticated,
    login,
    logout,
    getValidToken,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider")
  }
  return context
}

