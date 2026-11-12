/**
 * Reads the "email" claim from a token, only to show it on screen. The token is not verified here:
 * the backend verifies it on every request, which is what protects the data.
 */
export function emailFromToken(token: string | null): string | null {
  if (!token)
    return null

  try {
    // A JWT is header.payload.signature; the payload is JSON in base64url.
    const payload = token.split('.')[1]
    const data: unknown = JSON.parse(atob(payload.replace(/-/g, '+').replace(/_/g, '/')))

    if (typeof data === 'object' && data !== null && 'email' in data && typeof data.email === 'string')
      return data.email

    return null
  } catch {
    return null
  }
}
