import React, { useState, useRef } from 'react';
import { motion } from 'framer-motion';
import {
  Sparkles,
  ShieldCheck,
  Sliders,
  BarChart3,
  CheckCircle2,
  UserCheck,
  AlertTriangle,
  ArrowRight,
  Cpu
} from 'lucide-react';
import { RadialConfidenceGauge } from '../components/RadialConfidenceGauge';

interface AiGovernanceTabProps {
  userRole: string;
  recommendations: any[];
  demandForecast: any[];
  selectedRecommendation: any | null;
  onSelectRecommendation: (rec: any) => void;
  onSubmitGovernanceReview: (payload: {
    recommendationId: string;
    decisionStatus: string;
    overriddenUrgency: string;
    overriddenCategory: string;
    justification: string;
  }) => Promise<void>;
}

const decisionOptions = [
  { value: 'Approved', label: '✓ Agree with AI Score', style: 'border-emerald-500/50 bg-emerald-500/20 text-emerald-200' },
  { value: 'Modified', label: '✎ Change Urgency Level', style: 'border-indigo-500/50 bg-indigo-500/20 text-indigo-200' },
  { value: 'Rejected', label: '✕ Reject AI Suggestion', style: 'border-rose-500/50 bg-rose-500/20 text-rose-200' },
  { value: 'Deferred', label: '⏳ Review Later', style: 'border-amber-500/50 bg-amber-500/20 text-amber-200' }
];

const urgencyPills = ['Low', 'Medium', 'High', 'Critical', 'LifeThreatening'];

function getFriendlyFactorLabel(factorKey: string): string {
  switch (factorKey) {
    case 'MedicalKeywordScore':
      return 'Medical & Vulnerability Keywords';
    case 'PeopleCountScore':
      return 'Number of People Affected';
    case 'UserUrgencyScore':
      return 'Citizen Reported Urgency';
    case 'CategoryBaseScore':
      return 'Aid Category Importance';
    default:
      return factorKey;
  }
}

export const AiGovernanceTab: React.FC<AiGovernanceTabProps> = ({
  userRole,
  recommendations,
  demandForecast,
  selectedRecommendation,
  onSelectRecommendation,
  onSubmitGovernanceReview
}) => {
  const [action, setAction] = useState('Approved');
  const [urgency, setUrgency] = useState('High');
  const [category, setCategory] = useState('Medicine');
  const [justification, setJustification] = useState('');
  const reviewFormRef = useRef<HTMLDivElement | null>(null);

  const canApproveOrOverrideAi =
    userRole === 'EmergencyCoordinator' ||
    userRole === 'CaseManager' ||
    userRole === 'OrganizationAdmin' ||
    userRole === 'SystemAdmin';

  const handleReviewSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRecommendation) return;
    await onSubmitGovernanceReview({
      recommendationId: selectedRecommendation.id,
      decisionStatus: action,
      overriddenUrgency: urgency,
      overriddenCategory: category,
      justification
    });
    setJustification('');
  };

  const demandForecastSection = (
    <div className="bento-glass rounded-3xl p-5 sm:p-6">
      <div className="flex flex-wrap items-center justify-between gap-3 mb-5 pb-4 border-b border-white/[0.08]">
        <div className="flex items-center gap-3">
          <div className="w-10 h-10 rounded-2xl bg-indigo-500/20 border border-indigo-500/40 text-indigo-300 flex items-center justify-center shrink-0">
            <BarChart3 className="w-5 h-5" />
          </div>
          <div>
            <span className="text-xs font-bold uppercase tracking-wider text-indigo-400">
              Next 48 Hours Supply Outlook
            </span>
            <h3 className="text-lg font-extrabold text-white">
              Predicted Supply Needs & Warehouse Shortages (Next 2 Days)
            </h3>
            <p className="text-xs text-zinc-400">
              Compares expected community demand over the next 48 hours against current warehouse stock so you can spot shortages early.
            </p>
          </div>
        </div>
        <span className="px-3.5 py-1 rounded-full bg-indigo-500/15 border border-indigo-500/30 text-indigo-300 text-xs font-bold">
          48-Hour Forecast
        </span>
      </div>

      {demandForecast.length > 0 && (() => {
        const maxVal = Math.max(
          1,
          ...demandForecast.map((d) => Math.max(Number(d.predictedNext48HoursDemand || 0), Number(d.currentAvailableStock || 0)))
        );
        return (
          <div className="mb-5 p-4 rounded-2xl bg-black/20 border border-white/[0.07] space-y-3">
            <div className="flex flex-wrap items-center justify-between gap-2 text-xs">
              <span className="font-extrabold text-zinc-200">
                48h Predicted Demand vs. Current Warehouse Stock
              </span>
              <div className="flex items-center gap-4 text-[11px] font-bold">
                <span className="inline-flex items-center gap-1.5 text-indigo-300">
                  <span className="w-2.5 h-2.5 rounded-sm bg-indigo-500 inline-block" />
                  Predicted 48h Need
                </span>
                <span className="inline-flex items-center gap-1.5 text-emerald-300">
                  <span className="w-2.5 h-2.5 rounded-sm bg-emerald-500 inline-block" />
                  Available Stock
                </span>
              </div>
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 text-xs">
              {demandForecast.slice(0, 8).map((item) => {
                const needPct = Math.round((Number(item.predictedNext48HoursDemand || 0) / maxVal) * 100);
                const stockPct = Math.round((Number(item.currentAvailableStock || 0) / maxVal) * 100);
                return (
                  <div key={`chart-${item.categoryName}`} className="p-3 rounded-xl bg-white/[0.025] border border-white/[0.06] space-y-2">
                    <div className="flex items-center justify-between gap-1">
                      <span className="font-bold text-white truncate">{item.categoryName}</span>
                      <span className="font-mono text-[11px] text-zinc-400">
                        {item.currentAvailableStock}/{item.predictedNext48HoursDemand}
                      </span>
                    </div>
                    <div className="space-y-1.5">
                      <div className="w-full h-2 rounded-full bg-white/10 overflow-hidden">
                        <div className="h-full rounded-full bg-indigo-500 transition-all duration-500" style={{ width: `${Math.max(4, needPct)}%` }} />
                      </div>
                      <div className="w-full h-2 rounded-full bg-white/10 overflow-hidden">
                        <div className="h-full rounded-full bg-emerald-500 transition-all duration-500" style={{ width: `${Math.max(4, stockPct)}%` }} />
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        );
      })()}

      <div className="overflow-x-auto rounded-2xl border border-white/[0.08]">
        <table className="w-full text-left text-xs">
          <thead>
  );
};
