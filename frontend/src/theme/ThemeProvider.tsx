import { useCallback, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { ThemeContext } from './useTheme'
import type { ThemeChoice } from './useTheme'

const STORAGE_KEY = 'control-de-dinero.theme' // same key as the script in index.html

function readChoice(): ThemeChoice {
  try {
    const saved = localStorage.getItem(STORAGE_KEY)
    return saved === 'light' || saved === 'dark' ? saved : 'system'
  } catch {
    return 'system'
  }
}

/** Light, dark or "follow the phone". Puts or removes the "dark" class on <html>. */
export function ThemeProvider({ children }: { children: ReactNode }) {
  const [choice, setChoiceState] = useState<ThemeChoice>(readChoice)

  const setChoice = useCallback((next: ThemeChoice) => {
    try {
      localStorage.setItem(STORAGE_KEY, next)
    } catch {
      // Storage blocked: the choice works until the app is closed.
    }
    setChoiceState(next)
  }, [])

  useEffect(() => {
    const media = window.matchMedia('(prefers-color-scheme: dark)')
    const apply = () => {
      const dark = choice === 'dark' || (choice === 'system' && media.matches)
      document.documentElement.classList.toggle('dark', dark)
    }

    apply()
    media.addEventListener('change', apply) // the phone switched its own theme
    return () => media.removeEventListener('change', apply)
  }, [choice])

  const value = useMemo(() => ({ choice, setChoice }), [choice, setChoice])
  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}
