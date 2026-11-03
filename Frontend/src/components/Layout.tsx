import clsx from 'clsx';
import { BookA, GraduationCap, Layers, LayoutDashboard, Moon, Sun, TerminalSquare } from 'lucide-react';
import { NavLink, Outlet } from 'react-router-dom';
import { useI18n, useTheme } from '../i18n/I18nProvider';
import type { StringKey } from '../i18n/strings';

const NAV: { to: string; label: StringKey; icon: typeof LayoutDashboard }[] = [
  { to: '/', label: 'nav.dashboard', icon: LayoutDashboard },
  { to: '/learn', label: 'nav.learn', icon: GraduationCap },
  { to: '/glossary', label: 'nav.glossary', icon: BookA },
  { to: '/flashcards', label: 'nav.flashcards', icon: Layers },
  { to: '/playground', label: 'nav.playground', icon: TerminalSquare },
];

export function Layout() {
  const { t, lang, setLang } = useI18n();
  const { theme, toggle } = useTheme();

  return (
    <div className="flex min-h-dvh flex-col">
      <header className="sticky top-0 z-40 border-b border-line bg-bg/85 backdrop-blur-md">
        <div className="mx-auto flex h-14 max-w-7xl items-center gap-4 px-4">
          <NavLink to="/" className="flex shrink-0 items-center gap-2.5">
            <span dir="ltr" className="grid size-8 place-items-center rounded-lg bg-accent font-mono text-sm font-bold text-white">C#</span>
            <span className="hidden leading-tight sm:block">
              <span className="block text-sm font-bold">{t('app.name')}</span>
              <span className="block text-[11px] text-muted">{t('app.tagline')}</span>
            </span>
          </NavLink>

          <nav className="ms-4 hidden items-center gap-1 md:flex" aria-label="Main">
            {NAV.map(({ to, label, icon: Icon }) => (
              <NavLink
                key={to}
                to={to}
                end={to === '/'}
                className={({ isActive }) =>
                  clsx(
                    'flex items-center gap-2 rounded-lg px-3 py-1.5 text-sm transition-colors',
                    isActive ? 'bg-accent-soft font-medium text-accent' : 'text-muted hover:bg-surface-2 hover:text-text',
                  )
                }
              >
                <Icon className="size-4" />
                {t(label)}
              </NavLink>
            ))}
          </nav>

          <div className="ms-auto flex items-center gap-1.5">
            <button
              onClick={() => setLang(lang === 'ar' ? 'en' : 'ar')}
              className="h-9 rounded-lg border border-line px-3 text-sm font-medium hover:bg-surface-2"
              aria-label="Switch language"
            >
              {t('lang.switch')}
            </button>
            <button
              onClick={toggle}
              className="grid size-9 place-items-center rounded-lg border border-line hover:bg-surface-2"
              aria-label={theme === 'dark' ? t('theme.light') : t('theme.dark')}
              title={theme === 'dark' ? t('theme.light') : t('theme.dark')}
            >
              {theme === 'dark' ? <Sun className="size-4" /> : <Moon className="size-4" />}
            </button>
          </div>
        </div>
      </header>

      <main className="flex-1 pb-24 md:pb-10">
        <Outlet />
      </main>

      {/* Mobile bottom navigation */}
      <nav className="fixed inset-x-0 bottom-0 z-40 border-t border-line bg-surface/95 backdrop-blur-md md:hidden" aria-label="Main">
        <div className="grid grid-cols-5">
          {NAV.map(({ to, label, icon: Icon }) => (
            <NavLink
              key={to}
              to={to}
              end={to === '/'}
              className={({ isActive }) =>
                clsx('flex flex-col items-center gap-0.5 py-2.5 text-[11px]', isActive ? 'text-accent' : 'text-muted')
              }
            >
              <Icon className="size-5" />
              {t(label)}
            </NavLink>
          ))}
        </div>
      </nav>
    </div>
  );
}

export function PageHeader({ title, subtitle, children }: { title: string; subtitle?: string; children?: React.ReactNode }) {
  return (
    <div className="mb-8 flex flex-wrap items-end justify-between gap-4">
      <div>
        <h1 className="text-2xl font-bold sm:text-3xl">{title}</h1>
        {subtitle && <p className="mt-2 max-w-2xl text-muted">{subtitle}</p>}
      </div>
      {children}
    </div>
  );
}
