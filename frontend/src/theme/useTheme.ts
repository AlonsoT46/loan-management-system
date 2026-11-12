import { createContext, useContext } from 'react'

export type ThemeChoice = 'light' | 'dark' | 'system'

export interface ThemeState {
  choice: ThemeChoice
  setChoice: (choice: ThemeChoice) => void
}

export const ThemeContext = createContext<ThemeState | null>(null)

export function useTheme(): ThemeState {
  const theme = useContext(ThemeContext)
  if (!theme)
    throw new Error('useTheme must be used inside <ThemeProvider>.')
  return theme
}
