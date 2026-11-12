import { createContext, useContext } from 'react'

export interface AuthState {
  isLoggedIn: boolean
  /** Email of the logged-in user, read from the token (null when logged out). */
  email: string | null
  login: (email: string, password: string) => Promise<void>
  logout: () => void
}

// "Who is logged in", shared by the whole app. AuthProvider fills it; any screen reads it with useAuth().
// It plays the role ICurrentUser plays in the backend.
export const AuthContext = createContext<AuthState | null>(null)

export function useAuth(): AuthState {
  const auth = useContext(AuthContext)
  if (!auth)
    throw new Error('useAuth must be used inside <AuthProvider>.')
  return auth
}
