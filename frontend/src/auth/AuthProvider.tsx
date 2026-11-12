import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { api, onUnauthorized, tokenStorage } from '../api/client'
import { emailFromToken } from '../utils/jwt'
import { AuthContext } from './useAuth'

/** Keeps the session (the token) and offers login and logout to every screen. */
export function AuthProvider({ children }: { children: ReactNode }) {
  // Read once on start: if a token was saved, the user is still logged in.
  const [token, setToken] = useState<string | null>(() => tokenStorage.get())

  const logout = useCallback(() => {
    tokenStorage.clear()
    setToken(null)
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const response = await api.login(email, password)
    tokenStorage.set(response.token)
    setToken(response.token)
  }, [])

  // If the API answers 401 (expired token, password changed elsewhere...), log out.
  useEffect(() => {
    onUnauthorized(logout)
    return () => onUnauthorized(null)
  }, [logout])

  const value = useMemo(
    () => ({ isLoggedIn: token !== null, email: emailFromToken(token), login, logout }),
    [token, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
