import type { ReactNode } from 'react';
import { MousePointerClick } from 'lucide-react';
import { useI18n } from '../i18n/I18nProvider';
import type { LocalizedText } from '../lib/types';
import { AsyncTimeline } from './AsyncTimeline';
import { BuildPipeline } from './BuildPipeline';
import { ConfigLayers } from './ConfigLayers';
import { LinqPipeline } from './LinqPipeline';
import { ProjectExplorer } from './ProjectExplorer';
import { StackHeap } from './StackHeap';

const VISUALS: Record<string, () => ReactNode> = {
  'stack-heap': () => <StackHeap />,
  'linq-pipeline': () => <LinqPipeline />,
  'async-timeline': () => <AsyncTimeline />,
  'build-pipeline': () => <BuildPipeline />,
  'project-explorer': () => <ProjectExplorer />,
  'config-layers': () => <ConfigLayers />,
};

export default function Visual({ name, caption }: { name: string; caption?: LocalizedText }) {
  const { pick } = useI18n();
  const render = VISUALS[name];
  return (
    <figure className="overflow-hidden rounded-2xl border border-accent/30 bg-surface shadow-[0_0_0_4px_var(--accent-soft)]">
      {caption && (
        <figcaption className="flex items-center gap-2 border-b border-line bg-accent-soft/40 px-4 py-2.5 text-sm font-medium">
          <MousePointerClick className="size-4 shrink-0 text-accent" />
          {pick(caption)}
        </figcaption>
      )}
      <div className="p-4">{render ? render() : <p className="text-muted">Unknown visual: {name}</p>}</div>
    </figure>
  );
}

/** Small shared step controls used by several visuals. */
export function StepControls({
  step,
  total,
  onPrev,
  onNext,
  onReset,
  extra,
}: {
  step: number;
  total: number;
  onPrev: () => void;
  onNext: () => void;
  onReset: () => void;
  extra?: ReactNode;
}) {
  const { t } = useI18n();
  return (
    <div className="flex flex-wrap items-center gap-2">
      <button onClick={onPrev} disabled={step === 0} className="h-9 rounded-lg border border-line px-3 text-sm hover:bg-surface-2 disabled:opacity-40">
        {t('common.previous')}
      </button>
      <button onClick={onNext} disabled={step >= total} className="h-9 rounded-lg bg-accent px-4 text-sm font-medium text-white hover:brightness-110 disabled:opacity-40">
        {t('common.next')}
      </button>
      <button onClick={onReset} className="h-9 rounded-lg px-3 text-sm text-muted hover:text-text">
        ↺
      </button>
      <span className="text-sm text-muted tabular-nums" dir="ltr">
        {step} / {total}
      </span>
      {extra}
    </div>
  );
}
