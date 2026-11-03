import { Link, Route, Routes } from 'react-router-dom';
import { Layout } from './components/Layout';
import { useI18n } from './i18n/I18nProvider';
import { CurriculumPage } from './pages/CurriculumPage';
import { DashboardPage } from './pages/DashboardPage';
import { ExercisePage } from './pages/ExercisePage';
import { FlashcardsPage } from './pages/FlashcardsPage';
import { GlossaryPage } from './pages/GlossaryPage';
import { LessonPage } from './pages/LessonPage';
import { PlaygroundPage } from './pages/PlaygroundPage';

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<DashboardPage />} />
        <Route path="learn" element={<CurriculumPage />} />
        <Route path="lessons/:id" element={<LessonPage />} />
        <Route path="exercises/:id" element={<ExercisePage />} />
        <Route path="glossary" element={<GlossaryPage />} />
        <Route path="flashcards" element={<FlashcardsPage />} />
        <Route path="playground" element={<PlaygroundPage />} />
        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  );
}

function NotFound() {
  const { t } = useI18n();
  return (
    <div className="py-24 text-center">
      <p className="text-6xl font-bold text-accent">404</p>
      <p className="mt-3 text-muted">{t('notFound')}</p>
      <Link to="/" className="mt-6 inline-block text-accent hover:underline">
        {t('nav.dashboard')}
      </Link>
    </div>
  );
}
