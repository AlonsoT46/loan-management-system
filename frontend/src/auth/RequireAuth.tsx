import { Navigate, Outlet } from 'react-router'
import { useAuth } from './useAuth'

/** Guard for private screens: without a session it sends you to the login. */
export function RequireAuth() {
  const { isLoggedIn } = useAuth()
  return isLoggedIn ? <Outlet /> : <Navigate to="/login" replace />
}
