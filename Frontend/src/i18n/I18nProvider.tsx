import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import type { LocalizedText } from '../lib/types';
import { fixBidi } from '../lib/bidi';
import { strings, type Lang, type StringKey } from './strings';

interface I18n {
  lang: Lang;
  dir: 'rtl' | 'ltr';
  setLang: (lang: Lang) => void;
  t: (key: StringKey) => string;
  /** Picks the text for the current language; falls back to Arabic when no English version exists. */
  pick: (text: LocalizedText | null | undefined) => string;
  /** True when the UI is English but this text only exists in Arabic. */
  isFallback: (text: LocalizedText | null | undefined) => boolean;
}

const I18nContext = createContext<I18n | null>(null);

function readStored<T extends string>(key: string, fallback: T, allowed: readonly T[]): T {
  try {
    const value = localStorage.getItem(key) as T | null;
    return value && allowed.includes(value) ? value : fallback;
  } catch {
    return fallback;
  }
}

export function I18nProvider({ children }: { children: ReactNode }) {
  const [lang, setLangState] = useState<Lang>(() => readStored<Lang>('lb-lang', 'ar', ['ar', 'en']));

  useEffect(() => {
    document.documentElement.lang = lang;
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
    try {
      localStorage.setItem('lb-lang', lang);
    } catch {
      /* private mode */
    }
  }, [lang]);

  const t = useCallback((key: StringKey) => fixBidi(strings[lang][key] ?? strings.ar[key] ?? key), [lang]);
  const pick = useCallback(
    (text: LocalizedText | null | undefined) => (text ? fixBidi(lang === 'en' && text.en ? text.en : text.ar) : ''),
    [lang],
  );
  const isFallback = useCallback((text: LocalizedText | null | undefined) => lang === 'en' && !!text && !text.en, [lang]);

  const value = useMemo<I18n>(
    () => ({ lang, dir: lang === 'ar' ? 'rtl' : 'ltr', setLang: setLangState, t, pick, isFallback }),
    [lang, t, pick, isFallback],
  );
  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export function useI18n() {
  const ctx = useContext(I18nContext);
  if (!ctx) throw new Error('useI18n must be used inside <I18nProvider>');
  return ctx;
}

// ---------- Theme ----------
type Theme = 'dark' | 'light';
const ThemeContext = createContext<{ theme: Theme; toggle: () => void } | null>(null);

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setTheme] = useState<Theme>(() => readStored<Theme>('lb-theme', 'dark', ['dark', 'light']));
  useEffect(() => {
    document.documentElement.classList.toggle('dark', theme === 'dark');
    document.querySelector('meta[name="theme-color"]')?.setAttribute('content', theme === 'dark' ? '#0c0f16' : '#f6f6fa');
    try {
      localStorage.setItem('lb-theme', theme);
    } catch {
      /* private mode */
    }
  }, [theme]);
  const value = useMemo(() => ({ theme, toggle: () => setTheme((t) => (t === 'dark' ? 'light' : 'dark')) }), [theme]);
  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error('useTheme must be used inside <ThemeProvider>');
  return ctx;
}
