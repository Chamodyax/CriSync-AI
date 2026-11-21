import React, { useState, useEffect } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  FilePlus2,
  Home,
  ShieldAlert,
  Building2,
  Star,
  MessageSquare,
  Send,
  X,
  Upload,
  MapPin,
  HeartPulse,
  CheckCircle2,
  History,
  Sparkles,
  QrCode,
  Wand2,
  ChevronDown,
  ChevronUp,
  Globe,
  Bot
} from 'lucide-react';
import {
  LANGUAGE_OPTIONS,
  SupportedLanguage,
  TRANSLATIONS,
  QuickTemplateItem,
  generateAutomatedAiSupportReply
} from '../i18n';

interface CitizenPortalTabProps {
  alerts: any[];
  crises?: any[];
  shelters: any[];
  requests: any[];
  household: any | null;
  currentUserId?: string;
  briefing?: any | null;
  activeLanguage?: SupportedLanguage;
  onChangeLanguage?: (lang: SupportedLanguage) => void;
  onUploadEvidence: (file: File) => Promise<string | null>;
  onSubmitRequest: (payload: any) => Promise<void>;
  onSaveHousehold: (payload: any) => Promise<void>;
  onSubmitFeedback: (payload: {
    assistanceRequestId: string;
    rating: number;
    comments: string;
    wasAidTimely: boolean;
    wasQuantitySufficient: boolean;
  }) => Promise<void>;
}

const categoryKeys = [
  'Medicine',
  'Food',
  'WaterAndSanitation',
  'TemporaryShelter',
  'SearchAndRescue',
  'AccessibilityAssistance',
  'Transportation'
];

const urgencyStyles: Record<string, string> = {
  Low: 'border-zinc-500/40 text-zinc-300 bg-zinc-500/10',
  Medium: 'border-sky-500/40 text-sky-300 bg-sky-500/10',
  High: 'border-amber-500/40 text-amber-300 bg-amber-500/10',
  Critical: 'border-orange-500/40 text-orange-300 bg-orange-500/15',
  LifeThreatening: 'border-rose-500/50 text-rose-300 bg-rose-500/20'
};

export const CitizenPortalTab: React.FC<CitizenPortalTabProps> = ({
  alerts,
  crises = [],
  shelters,
  requests,
  household,
  currentUserId,
  briefing,
  activeLanguage = 'en',
  onChangeLanguage,
  onUploadEvidence,
  onSubmitRequest,
  onSaveHousehold,
  onSubmitFeedback
}) => {
  const [activeSection, setActiveSection] = useState<'all' | 'request' | 'ai-support' | 'track' | 'family' | 'shelters'>('all');
  const [showExactGps, setShowExactGps] = useState(false);
  const [language, setLanguage] = useState<SupportedLanguage>(activeLanguage || 'en');

  const [selectedCrisisId, setSelectedCrisisId] = useState('');
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [category, setCategory] = useState('Food');
  const [urgency, setUrgency] = useState('High');
  const [peopleCount, setPeopleCount] = useState(1);
  const [quantity, setQuantity] = useState(1);
  const [accessibility, setAccessibility] = useState(false);
  const [address, setAddress] = useState('');
  const [latitude, setLatitude] = useState(6.9271);
  const [longitude, setLongitude] = useState(79.8612);
  const [evidenceFileName, setEvidenceFileName] = useState('');

  const [headName, setHeadName] = useState('');
  const [totalMembers, setTotalMembers] = useState(1);
  const [childrenCount, setChildrenCount] = useState(0);
  const [elderlyCount, setElderlyCount] = useState(0);
  const [hasMobility, setHasMobility] = useState(false);
  const [hasChronicMedical, setHasChronicMedical] = useState(false);
  const [medicalNotes, setMedicalNotes] = useState('');

  const [feedbackRequestId, setFeedbackRequestId] = useState<string | null>(null);
  const [rating, setRating] = useState(5);
  const [feedbackComments, setFeedbackComments] = useState('');

  const [selectedFaqId, setSelectedFaqId] = useState<string>('shelter');
  const [customAiQuery, setCustomAiQuery] = useState('');
  const [customAiReply, setCustomAiReply] = useState<string | null>(null);

  useEffect(() => {
    if (activeLanguage && activeLanguage !== language) {
      setLanguage(activeLanguage);
    }
  }, [activeLanguage]);

  useEffect(() => {
    if (household) {
      setHeadName(household.householdHeadName || '');
      setTotalMembers(household.totalMembers || 1);
      setChildrenCount(household.childrenCount ?? 0);
      setElderlyCount(household.elderlyCount ?? 0);
      setHasMobility(Boolean(household.hasMobilityNeeds));
      setHasChronicMedical(Boolean(household.hasChronicMedicalNeeds));
      setMedicalNotes(household.medicalNotes || '');
      if (household.primaryAddress) {
        setAddress(household.primaryAddress);
      }
    }
  }, [household]);

  const activeCrisisId = selectedCrisisId || crises[0]?.id || '';

  const handleSelectLanguage = (newLang: SupportedLanguage) => {
    setLanguage(newLang);
    setCustomAiReply(null);
    if (onChangeLanguage) {
      onChangeLanguage(newLang);
    }
  };

  const t = TRANSLATIONS[language] || TRANSLATIONS.en;

  const applyQuickTemplate = (tpl: QuickTemplateItem) => {
    setCategory(tpl.category);
    setUrgency(tpl.urgency);
    setTitle(tpl.title);
    setDescription(tpl.description);
    setPeopleCount(tpl.people);
    setQuantity(tpl.qty);
    setAccessibility(tpl.wheelchair);
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (!selectedFile) return;
    const safeName = await onUploadEvidence(selectedFile);
    if (safeName) {
      setEvidenceFileName(safeName);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onSubmitRequest({
      crisisEventId: activeCrisisId,
      category,
      selfReportedUrgency: urgency,
      title,
      description,
      affectedPeopleCount: Number(peopleCount),
      requestedQuantity: Number(quantity),
      requiresAccessibilitySupport: accessibility,
      locationAddress: address,
      latitude: Number(latitude),
      longitude: Number(longitude),
      supportingEvidenceFileName: evidenceFileName,
      supportingEvidenceMimeType: 'image/jpeg',
      preferredLanguage: language,
      idempotencyKey: `REQ-IDEMP-${Date.now()}-${title.trim().toLowerCase().replace(/\s+/g, '-')}`
    });
    setTitle('');
    setDescription('');
  };

  const handleHouseholdSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await onSaveHousehold({
      householdHeadName: headName,
      totalMembers: Number(totalMembers),
      childrenCount: Number(childrenCount),
      elderlyCount: Number(elderlyCount),
      hasMobilityNeeds: hasMobility,
      hasChronicMedicalNeeds: hasChronicMedical,
      medicalNotes,
      primaryAddress: address,
      latitude: Number(latitude),
      longitude: Number(longitude)
    });
  };

  const handleFeedbackSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!feedbackRequestId) return;
    await onSubmitFeedback({
      assistanceRequestId: feedbackRequestId,
      rating: Number(rating),
      comments: feedbackComments,
      wasAidTimely: true,
      wasQuantitySufficient: true
    });
    setFeedbackRequestId(null);
  };

  const handleAskCustomAiQuestion = (e: React.FormEvent) => {
    e.preventDefault();
    if (!customAiQuery.trim()) return;
    const reply = generateAutomatedAiSupportReply(language, customAiQuery);
    setCustomAiReply(reply);
  };

  const myRequests = requests.filter(
    (r) => !currentUserId || r.citizenUserId === currentUserId || r.referenceCode?.startsWith('REQ-')
  );

  const activeFaqObj = t.citizen.aiFaqs.find((f) => f.id === selectedFaqId) || t.citizen.aiFaqs[0];

  return (
    <div className="space-y-6" dir={t.dir}>
      <div className="rounded-3xl overflow-hidden relative min-h-[168px] sm:min-h-[196px] border border-white/10 shadow-lg group">
        <img
          src="/images/hero-relief.jpg"
          alt="Humanitarian emergency relief teams assisting families"
          className="absolute inset-0 w-full h-full object-cover object-center group-hover:scale-105 transition-transform duration-700"
        />
        <div className="crisync-photo-scrim-side absolute inset-0" />
        <div className="keep-white relative z-10 p-5 sm:p-7 flex flex-col justify-between min-h-[168px] sm:min-h-[196px]">
          <div className="flex flex-wrap items-center gap-2">
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-emerald-500/25 border border-emerald-400/50 text-[11px] font-extrabold text-white backdrop-blur-md">
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
              24/7 Humanitarian Support • You Are Safe Here
            </span>
          </div>
          <div className="mt-4 space-y-2 max-w-2xl">
            <h2 className="text-xl sm:text-2xl font-black text-white tracking-tight leading-snug">
              {t.citizen.portalTitle}
            </h2>
            <p className="text-xs sm:text-sm text-zinc-100 leading-relaxed">
              {t.citizen.portalSubtitle}
            </p>
            <div className="flex flex-wrap items-center gap-2 pt-1">
              <span className="px-2.5 py-1 rounded-lg bg-white/15 border border-white/25 text-[11px] font-bold text-white backdrop-blur-sm">
                {shelters.length} Safe Shelters Open
              </span>
              <span className="px-2.5 py-1 rounded-lg bg-white/15 border border-white/25 text-[11px] font-bold text-white backdrop-blur-sm">
                6 Languages Supported
              </span>
              <span className="px-2.5 py-1 rounded-lg bg-white/15 border border-white/25 text-[11px] font-bold text-white backdrop-blur-sm">
                Instant AI & Human Aid
              </span>
            </div>
          </div>
        </div>
      </div>

      <div className="bento-glass rounded-3xl p-4 sm:p-5 space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="text-base sm:text-lg font-extrabold text-white">
              {t.citizen.portalTitle}
            </h1>
            <p className="text-xs text-zinc-400">
              {t.citizen.portalSubtitle}
            </p>
          </div>

          <div className="mobile-tabs-scroll flex items-center gap-1.5 p-1.5 rounded-2xl bg-indigo-500/10 border border-indigo-500/30 overflow-x-auto max-w-full">
            <span className="text-[11px] font-bold text-indigo-300 px-2 flex items-center gap-1 shrink-0">
              <Globe className="w-3.5 h-3.5" />
              <span className="hidden sm:inline">{t.languageLabel}:</span>
            </span>
            {LANGUAGE_OPTIONS.map((l) => (
              <button
                key={l.code}
                type="button"
                onClick={() => handleSelectLanguage(l.code)}
                className={`shrink-0 px-2.5 py-1 rounded-xl text-xs font-bold transition ${
                  language === l.code
                    ? 'bg-indigo-600 text-white keep-white shadow-sm'
                    : 'text-zinc-300 hover:text-white hover:bg-white/10'
                }`}
              >
                {l.label}
              </button>
            ))}
          </div>
        </div>

        <div className="mobile-tabs-scroll flex items-center gap-1.5 bg-white/[0.03] p-1.5 rounded-2xl border border-white/10 overflow-x-auto">
          {[
            { id: 'all', label: t.citizen.tabAll },
            { id: 'request', label: t.citizen.tabRequest },
            { id: 'ai-support', label: t.citizen.tabAiSupport },
            { id: 'track', label: `${t.citizen.tabTrack} (${myRequests.length})` },
            { id: 'family', label: t.citizen.tabFamily },
            { id: 'shelters', label: `${t.citizen.tabShelters} (${shelters.length})` }
          ].map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => setActiveSection(tab.id as any)}
              className={`shrink-0 px-3 py-2 rounded-xl text-xs font-bold transition ${
                activeSection === tab.id
                  ? 'bg-indigo-600 text-white keep-white shadow-sm'
                  : 'text-zinc-400 hover:text-white hover:bg-white/[0.05]'
              }`}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>

      {(activeSection === 'all' || activeSection === 'ai-support') && (
        <div className="bento-glass rounded-3xl p-5 sm:p-6 border border-indigo-500/30 space-y-5">
          <div className="flex flex-wrap items-center justify-between gap-3 pb-4 border-b border-white/[0.08]">
            <div className="flex items-center gap-3">
              <div className="keep-white w-11 h-11 rounded-2xl bg-gradient-to-br from-indigo-600 to-purple-600 flex items-center justify-center text-white shadow-md shrink-0">
                <Bot className="w-5 h-5" />
              </div>
              <div>
                <span className="text-xs font-bold uppercase tracking-wider text-indigo-400 block">
                  {t.citizen.aiAssistantBadge}
                </span>
                <h2 className="text-lg font-extrabold text-white">
                  {t.citizen.aiAssistantTitle}
                </h2>
                <p className="text-xs text-zinc-400">
                  {t.citizen.aiAssistantSubtitle}
                </p>
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-1 p-1 rounded-xl bg-black/25 border border-white/10">
              {LANGUAGE_OPTIONS.map((l) => (
                <button
                  key={l.code}
                  type="button"
                  onClick={() => handleSelectLanguage(l.code)}
                  className={`px-2.5 py-1 rounded-lg text-xs font-bold transition ${
                    language === l.code
                      ? 'bg-indigo-600 text-white keep-white'
                      : 'text-zinc-400 hover:text-white'
                  }`}
                >
                  {l.label}
                </button>
              ))}
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-12 gap-5">
            {briefing && (
              <div className="lg:col-span-5 p-4 sm:p-5 rounded-2xl bg-indigo-500/10 border border-indigo-500/30 flex flex-col justify-between space-y-3">
                <div className="space-y-2">
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-[11px] font-bold uppercase tracking-wider text-indigo-400 flex items-center gap-1.5">
                      <Globe className="w-3.5 h-3.5" />
                      {t.citizen.aiOfficialBriefingTitle}
                    </span>
                    <span className="text-[11px] font-bold px-2.5 py-0.5 rounded-full bg-indigo-600 text-white keep-white">
                      {briefing.languageName}
                    </span>
                  </div>
                  <h3 className="text-sm sm:text-base font-extrabold text-white leading-snug">
                    {briefing.headline}
                  </h3>
                  <p className="text-xs text-zinc-300 leading-relaxed">
                    {briefing.operationalSummary}
                  </p>
                </div>

                <div className="p-3 rounded-xl bg-black/20 border border-indigo-500/30 text-xs text-zinc-200">
                  <strong className="text-indigo-300 block mb-0.5">{t.dashboard.safetyAdviceBadge}:</strong>
                  {briefing.citizenSafetyAdvisory}
                </div>
              </div>
            )}

            <div className={`${briefing ? 'lg:col-span-7' : 'lg:col-span-12'} space-y-4`}>
              <div className="flex flex-wrap gap-2">
                {t.citizen.aiFaqs.map((faq) => {
                  const isSelected = selectedFaqId === faq.id && !customAiReply;
                  return (
                    <button
                      key={faq.id}
                      type="button"
                      onClick={() => {
                        setSelectedFaqId(faq.id);
                        setCustomAiReply(null);
                      }}
                      className={`px-3 py-2 rounded-xl text-xs font-bold border text-left transition ${
                        isSelected
                          ? 'bg-indigo-600 border-indigo-500 text-white keep-white shadow-sm'
                          : 'bg-white/[0.03] border-white/10 text-zinc-300 hover:bg-white/[0.07]'
                      }`}
                    >
                      {faq.question}
                    </button>
                  );
                })}
              </div>

              <div className="p-4 rounded-2xl bg-emerald-500/10 border border-emerald-500/30 space-y-1.5">
                <div className="flex items-center gap-2 text-xs font-extrabold text-emerald-300">
                  <Sparkles className="w-4 h-4 text-emerald-400 shrink-0" />
                  <span>
                    {customAiReply ? customAiQuery : activeFaqObj.question}
                  </span>
                </div>
                <p className="text-xs sm:text-sm text-zinc-200 leading-relaxed">
                  {customAiReply || activeFaqObj.answer}
                </p>
              </div>

              <form onSubmit={handleAskCustomAiQuestion} className="flex gap-2">
                <input
                  type="text"
                  value={customAiQuery}
                  onChange={(e) => setCustomAiQuery(e.target.value)}
                  placeholder={t.citizen.aiAskPlaceholder}
                  className="rsvp-input flex-1 text-xs"
                />
                <button
                  type="submit"
                  className="keep-white px-4 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-extrabold flex items-center gap-1.5 shrink-0 shadow-sm"
                >
                  <Send className="w-3.5 h-3.5" />
                  <span>{t.citizen.aiAskBtn}</span>
                </button>
              </form>
            </div>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {(activeSection === 'all' || activeSection === 'request') && (
          <div className={`${activeSection === 'request' ? 'lg:col-span-12' : 'lg:col-span-7'} bento-glass rounded-3xl p-5 sm:p-7 relative overflow-hidden`}>
            <div className="flex flex-wrap items-start justify-between gap-3 mb-5 pb-4 border-b border-white/[0.08]">
              <div className="flex items-center gap-3">
                <div className="keep-white w-11 h-11 rounded-2xl bg-indigo-600 flex items-center justify-center text-white shadow-md shrink-0">
                  <FilePlus2 className="w-5 h-5" />
                </div>
                <div>
                  <span className="text-xs font-bold uppercase tracking-wider text-indigo-400">
                    {t.citizen.formBadge}
                  </span>
                  <h2 className="text-lg sm:text-xl font-extrabold text-white tracking-tight">
                    {t.citizen.formTitle}
                  </h2>
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-1 p-1 rounded-xl bg-black/30 border border-white/10">
                {LANGUAGE_OPTIONS.map((l) => (
                  <button
                    key={l.code}
                    type="button"
                    onClick={() => handleSelectLanguage(l.code)}
                    className={`px-2.5 py-1 rounded-lg text-xs font-bold transition ${
                      language === l.code
                        ? 'bg-indigo-600 text-white keep-white'
                        : 'text-zinc-400 hover:text-white'
                    }`}
                  >
                    {l.label}
                  </button>
                ))}
              </div>
            </div>

            <div className="mb-5 p-3.5 rounded-2xl bg-indigo-500/10 border border-indigo-500/25">
              <p className="text-xs font-bold text-indigo-300 flex items-center gap-1.5 mb-2">
                <Wand2 className="w-3.5 h-3.5" />
                {t.citizen.quickTemplatePrompt}
              </p>
              <div className="flex flex-wrap gap-2">
                {t.citizen.templates.map((tpl) => (
                  <button
                    key={tpl.label}
                    type="button"
                    onClick={() => applyQuickTemplate(tpl)}
                    className="px-3 py-1.5 rounded-xl bg-white/[0.06] hover:bg-indigo-600 hover:text-white text-xs font-bold text-zinc-200 border border-white/15 transition"
                  >
                    {tpl.label}
                  </button>
                ))}
              </div>
            </div>

            <form onSubmit={handleSubmit} className="space-y-4">
              {crises.length === 0 ? (
                <div className="p-3.5 rounded-2xl bg-amber-500/15 border border-amber-500/35 text-xs text-amber-200 font-semibold">
                  No active crisis event declared yet. An Emergency Coordinator or System Admin must declare a crisis first before emergency requests can be logged.
                </div>
              ) : (
                <div>
                  <label className="block text-xs font-bold text-zinc-300 mb-1.5">
                    Active Emergency Operation
                  </label>
                  <select
                    value={activeCrisisId}
                    onChange={(e) => setSelectedCrisisId(e.target.value)}
                    className="rsvp-input"
                  >
                    {crises.map((c) => (
                      <option key={c.id} value={c.id}>
                        {c.title} ({c.region} — {c.severity})
                      </option>
                    ))}
                  </select>
                </div>
              )}

              <div>
                <label className="block text-xs font-bold text-zinc-300 mb-2">
                  {t.citizen.step1Category}
                </label>
                <div className="flex flex-wrap gap-2">
                  {categoryKeys.map((catKey) => {
                    const active = category === catKey;
                    return (
                      <button
                        key={catKey}
                        type="button"
                        onClick={() => setCategory(catKey)}
                        className={`px-3.5 py-2 rounded-xl text-xs font-bold border transition-all ${
                          active
                            ? 'bg-indigo-600 border-indigo-500 text-white keep-white shadow-sm'
                            : 'bg-white/[0.03] border-white/10 text-zinc-300 hover:bg-white/[0.06]'
                        }`}
                      >
                        {t.citizen.categories[catKey] || catKey}
                      </button>
                    );
                  })}
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold text-zinc-300 mb-2">
                  {t.citizen.step2Urgency}
                </label>
                <div className="grid grid-cols-2 sm:grid-cols-5 gap-2">
                  {Object.keys(urgencyStyles).map((uKey) => {
                    const active = urgency === uKey;
                    const uMeta = t.citizen.urgencies[uKey] || { label: uKey, desc: '' };
                    return (
                      <button
                        key={uKey}
                        type="button"
                        onClick={() => setUrgency(uKey)}
                        className={`py-2 px-2.5 rounded-xl text-xs font-bold border transition-all text-center ${
                          active
                            ? `${urgencyStyles[uKey]} ring-2 ring-indigo-500/40 scale-[1.01]`
                            : 'bg-white/[0.02] border-white/10 text-zinc-400 hover:text-zinc-200'
                        }`}
                      >
                        <div className="font-extrabold">{uMeta.label}</div>
                        <div className="text-[10px] opacity-80 font-normal">{uMeta.desc}</div>
                      </button>
                    );
                  })}
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold text-zinc-300 mb-1.5">
                  {t.citizen.step3Summary}
                </label>
                <input
                  type="text"
                  required
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder="Short emergency summary headline"
                  className="rsvp-input"
                />
                <p className="text-[11px] text-zinc-400/75 opacity-75 mt-1 font-normal">
                  Example: Stranded family of 4 needing clean water and infant food packs
                </p>
              </div>

              <div>
                <label className="block text-xs font-bold text-zinc-300 mb-1.5">
                  {t.citizen.step4Description}
                </label>
                <textarea
                  rows={3}
                  required
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  placeholder="Describe access conditions, urgent medical conditions, and number of people"
                  className="rsvp-input"
                />
                <p className="text-[11px] text-zinc-400/75 opacity-75 mt-1 font-normal">
                  Example: Road submerged by 1 meter; elderly patient on blood-pressure medication cannot walk to main road.
                </p>
              </div>

              <div className="p-3.5 rounded-2xl bg-white/[0.02] border border-white/10 space-y-3">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <label className="text-xs font-bold text-zinc-300 flex items-center gap-1.5">
                    <MapPin className="w-4 h-4 text-indigo-400" />
                    {t.citizen.step5Location}
                  </label>
                </div>

                <input
                  type="text"
                  required
                  value={address}
                  onChange={(e) => setAddress(e.target.value)}
                  placeholder="Enter street address, village, or nearest landmark"
                  className="rsvp-input"
                />
                <p className="text-[11px] text-zinc-400/75 opacity-75 font-normal">
                  Example: 18 Temple Lane, near Municipal Water Tank (or expand GPS below)
                </p>

                <div>
                  <button
                    type="button"
                    onClick={() => setShowExactGps((v) => !v)}
                    className="text-[11px] font-semibold text-indigo-400 hover:underline flex items-center gap-1"
                  >
                    {showExactGps ? <ChevronUp className="w-3.5 h-3.5" /> : <ChevronDown className="w-3.5 h-3.5" />}
                    <span>
                      GPS ({latitude}, {longitude})
                    </span>
                  </button>

                  {showExactGps && (
                    <div className="grid grid-cols-2 gap-3 mt-2">
                      <div>
                        <label className="block text-[11px] font-semibold text-zinc-400 mb-1">
                          Latitude
                        </label>
                        <input
                          type="number"
                          step="0.0001"
                          value={latitude}
                          onChange={(e) => setLatitude(Number(e.target.value))}
                          className="rsvp-input font-mono"
                        />
                      </div>
                      <div>
                        <label className="block text-[11px] font-semibold text-zinc-400 mb-1">
                          Longitude
                        </label>
                        <input
                          type="number"
                          step="0.0001"
                          value={longitude}
                          onChange={(e) => setLongitude(Number(e.target.value))}
                          className="rsvp-input font-mono"
                        />
                      </div>
                    </div>
                  )}
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                <div>
                  <label className="block text-xs font-bold text-zinc-300 mb-1">
                    {t.citizen.peopleCountLabel}
                  </label>
                  <input
                    type="number"
                    min={1}
                    value={peopleCount}
                    onChange={(e) => setPeopleCount(Number(e.target.value))}
                    className="rsvp-input"
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-zinc-300 mb-1">
                    {t.citizen.quantityLabel}
                  </label>
                  <input
                    type="number"
                    min={1}
                    value={quantity}
                    onChange={(e) => setQuantity(Number(e.target.value))}
                    className="rsvp-input"
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-zinc-300 mb-1 flex items-center gap-1">
                    <Upload className="w-3.5 h-3.5 text-indigo-400" />
                    {t.citizen.photoLabel}
                  </label>
                  <input
                    type="file"
                    accept=".jpg,.jpeg,.png,.pdf"
                    onChange={handleFileChange}
                    className="w-full text-xs text-zinc-400 file:mr-2 file:py-1.5 file:px-3 file:rounded-xl file:border file:border-indigo-500/30 file:text-xs file:font-bold file:bg-indigo-500/20 file:text-indigo-200 hover:file:bg-indigo-500/30"
                  />
                </div>
              </div>

              <label className="flex items-center gap-3 text-xs sm:text-sm font-semibold text-zinc-200 cursor-pointer p-3.5 rounded-2xl bg-white/[0.03] border border-white/10 hover:border-indigo-500/40 transition">
                <input
                  type="checkbox"
                  checked={accessibility}
                  onChange={(e) => setAccessibility(e.target.checked)}
                  className="w-4 h-4 rounded border-white/20 bg-black/40 text-indigo-500"
                />
                <span>{t.citizen.wheelchairCheckbox}</span>
              </label>

              <motion.button
                whileHover={{ scale: 1.01 }}
                whileTap={{ scale: 0.985 }}
                type="submit"
                className="keep-white w-full py-3.5 rounded-2xl bg-indigo-600 hover:bg-indigo-500 text-white font-extrabold text-sm shadow-md flex items-center justify-center gap-2 transition"
              >
                <Send className="w-4 h-4" />
                <span>{t.citizen.submitRequestBtn}</span>
              </motion.button>
            </form>
          </div>
        )}

        {(activeSection === 'all' || activeSection === 'family' || activeSection === 'shelters') && (
          <div className={`${activeSection === 'all' ? 'lg:col-span-5' : 'lg:col-span-12'} space-y-5`}>
            {(activeSection === 'all' || activeSection === 'family') && (
              <div className="bento-glass rounded-3xl p-5 sm:p-6 relative overflow-hidden border border-emerald-500/25">
                <div className="flex items-center justify-between gap-2 mb-4 pb-3 border-b border-white/10">
                  <div className="flex items-center gap-2.5">
                    <div className="w-9 h-9 rounded-2xl bg-emerald-500/20 border border-emerald-500/40 text-emerald-300 flex items-center justify-center">
                      <Home className="w-4 h-4" />
                    </div>
                    <div>
                      <span className="text-[11px] font-bold uppercase tracking-wider text-emerald-400 block">
                        {t.citizen.familyBadge}
                      </span>
                      <h3 className="text-base font-extrabold text-white">{t.citizen.familyTitle}</h3>
                    </div>
                  </div>
                  <QrCode className="w-6 h-6 text-emerald-400/80" />
                </div>

                <form onSubmit={handleHouseholdSubmit} className="space-y-3 text-xs">
                  <div>
                    <label className="block text-xs font-bold text-zinc-300 mb-1">
                      {t.citizen.headNameLabel}
                    </label>
                    <input
                      type="text"
                      value={headName}
                      onChange={(e) => setHeadName(e.target.value)}
                      className="rsvp-input"
                    />
                  </div>

                  <div className="grid grid-cols-3 gap-2.5">
                    <div>
                      <label className="block text-xs font-bold text-zinc-300 mb-1">{t.citizen.totalMembersLabel}</label>
                      <input
                        type="number"
                        min={1}
                        value={totalMembers}
                        onChange={(e) => setTotalMembers(Number(e.target.value))}
                        className="rsvp-input"
                      />
                    </div>
                    <div>
                      <label className="block text-xs font-bold text-zinc-300 mb-1">{t.citizen.childrenLabel}</label>
                      <input
                        type="number"
                        min={0}
                        value={childrenCount}
                        onChange={(e) => setChildrenCount(Number(e.target.value))}
                        className="rsvp-input"
                      />
                    </div>
                    <div>
                      <label className="block text-xs font-bold text-zinc-300 mb-1">{t.citizen.elderlyLabel}</label>
                      <input
                        type="number"
                        min={0}
                        value={elderlyCount}
                        onChange={(e) => setElderlyCount(Number(e.target.value))}
                        className="rsvp-input"
                      />
                    </div>
                  </div>

                  <div>
                    <label className="block text-xs font-bold text-zinc-300 mb-1">
                      {t.citizen.medicalNotesLabel}
                    </label>
                    <textarea
                      rows={2}
                      value={medicalNotes}
                      onChange={(e) => setMedicalNotes(e.target.value)}
                      placeholder="Allergies, refrigerated insulin, oxygen tank, or dialysis schedule"
                      className="rsvp-input"
                    />
                    <p className="text-[11px] text-zinc-400/75 opacity-75 mt-1 font-normal">
                      Example: Elderly member requires daily insulin & wheelchair access during evacuation.
                    </p>
                  </div>

                  <div className="space-y-2 p-3 rounded-2xl bg-black/20 border border-white/[0.06]">
                    <label className="flex items-center gap-2.5 text-zinc-300 font-semibold cursor-pointer">
                      <input
                        type="checkbox"
                        checked={hasMobility}
                        onChange={(e) => setHasMobility(e.target.checked)}
                        className="w-4 h-4 rounded border-white/20 bg-black/40 text-emerald-500"
                      />
                      {t.citizen.mobilityCheckbox}
                    </label>
                    <label className="flex items-center gap-2.5 text-zinc-300 font-semibold cursor-pointer">
                      <input
                        type="checkbox"
                        checked={hasChronicMedical}
                        onChange={(e) => setHasChronicMedical(e.target.checked)}
                        className="w-4 h-4 rounded border-white/20 bg-black/40 text-emerald-500"
                      />
                      {t.citizen.chronicCheckbox}
                    </label>
                  </div>

                  <motion.button
                    whileTap={{ scale: 0.98 }}
                    type="submit"
                    className="keep-white w-full py-2.5 rounded-2xl bg-emerald-600 hover:bg-emerald-500 text-white font-bold flex items-center justify-center gap-1.5 transition shadow-sm"
                  >
                    <HeartPulse className="w-4 h-4" />
                    <span>{t.citizen.saveFamilyBtn}</span>
                  </motion.button>
                </form>
              </div>
            )}

            {(activeSection === 'all' || activeSection === 'shelters') && (
              <>
                <div className="bento-glass rounded-3xl p-5 border border-rose-500/30">
                  <h3 className="text-sm font-extrabold text-rose-300 flex items-center gap-2">
                    <ShieldAlert className="w-4 h-4 text-rose-400" />
                    {t.citizen.alertsTitle}
                  </h3>
                  {alerts.length === 0 ? (
                    <p className="text-xs text-zinc-400 mt-3 pt-3 border-t border-white/10">
                      No active public emergency broadcasts have been issued yet.
                    </p>
                  ) : (
                    alerts.map((alertItem) => (
                      <div key={alertItem.id} className="mt-3 pt-3 border-t border-white/10 text-xs space-y-1.5">
                        <p className="font-extrabold text-white text-sm">
                          {alertItem.title}
                        </p>
                        <p className="text-zinc-300 leading-relaxed">
                          {alertItem.message}
                        </p>
                        {alertItem.safetyGuidance && (
                          <p className="text-rose-200 font-semibold bg-rose-500/15 border border-rose-500/30 px-3 py-2 rounded-xl">
                            {t.citizen.safetyWhatToDo} {alertItem.safetyGuidance}
                          </p>
                        )}
                      </div>
                    ))
                  )}
                </div>

                <div className="bento-glass rounded-3xl p-5">
                  <h3 className="text-sm font-extrabold text-white mb-3 flex items-center gap-2">
                    <Building2 className="w-4 h-4 text-indigo-400" />
                    {t.citizen.sheltersTitle}
                  </h3>
                  {shelters.length === 0 ? (
                    <p className="text-xs text-zinc-400">
                      No evacuation shelters have been registered yet.
                    </p>
                  ) : (
                    <div className="space-y-2.5">
                      {shelters.map((shelter) => (
                        <div
                          key={shelter.id}
                          className="p-3.5 rounded-2xl bg-white/[0.03] border border-white/[0.07] text-xs"
                        >
                          <div className="flex items-center justify-between gap-2">
                            <p className="font-bold text-white text-sm">{shelter.name}</p>
                            <span className="font-bold text-xs text-emerald-300 bg-emerald-500/15 px-2.5 py-0.5 rounded-full border border-emerald-500/30 shrink-0">
                              {shelter.totalCapacity - shelter.currentOccupancy} {t.citizen.spacesOpen}
                            </span>
                          </div>
                          <p className="text-zinc-400 flex items-center gap-1 mt-1">
                            <MapPin className="w-3.5 h-3.5 text-indigo-400" />
                            {shelter.address}
                          </p>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </>
            )}
          </div>
        )}
      </div>

      {(activeSection === 'all' || activeSection === 'track') && (
        <div className="bento-glass rounded-3xl p-5 sm:p-6">
          <div className="flex flex-wrap items-center justify-between gap-2 mb-5 pb-4 border-b border-white/[0.08]">
            <div>
              <span className="text-xs font-bold uppercase tracking-wider text-indigo-400">
                {t.citizen.trackerBadge}
              </span>
              <h3 className="text-lg font-extrabold text-white">
                {t.citizen.trackerTitle}
              </h3>
            </div>
            <span className="px-3.5 py-1 rounded-full bg-indigo-500/15 border border-indigo-500/30 text-xs font-bold text-indigo-300">
              {myRequests.length}
            </span>
          </div>

          <AnimatePresence>
            {feedbackRequestId && (
              <motion.form
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: 'auto' }}
                exit={{ opacity: 0, height: 0 }}
                onSubmit={handleFeedbackSubmit}
                className="mb-5 p-5 rounded-3xl bg-emerald-500/10 border border-emerald-500/30 space-y-4 text-xs overflow-hidden"
              >
                <div className="flex items-center justify-between">
                  <p className="font-extrabold text-emerald-300 text-sm flex items-center gap-2">
                    <Sparkles className="w-4 h-4 text-emerald-400" />
                    {t.citizen.feedbackTitle}
                  </p>
                  <button
                    type="button"
                    onClick={() => setFeedbackRequestId(null)}
                    className="text-zinc-400 hover:text-white p-1"
                  >
                    <X className="w-4 h-4" />
                  </button>
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 items-center">
                  <div>
                    <label className="block font-bold text-zinc-300 mb-2">{t.citizen.ratingLabel}</label>
                    <div className="flex items-center gap-1.5">
                      {[1, 2, 3, 4, 5].map((star) => (
                        <button
                          key={star}
                          type="button"
                          onClick={() => setRating(star)}
                          className={`p-2 rounded-xl border transition ${
                            rating >= star
                              ? 'bg-amber-500/20 border-amber-400 text-amber-300'
                              : 'bg-black/30 border-white/10 text-zinc-600'
                          }`}
                        >
                          <Star className={`w-4 h-4 ${rating >= star ? 'fill-amber-400' : ''}`} />
                        </button>
                      ))}
                    </div>
                  </div>
                  <div className="sm:col-span-2">
                    <label className="block font-bold text-zinc-300 mb-1.5">{t.citizen.commentsLabel}</label>
                    <input
                      type="text"
                      required
                      value={feedbackComments}
                      onChange={(e) => setFeedbackComments(e.target.value)}
                      className="rsvp-input"
                    />
                  </div>
                </div>

                <button
                  type="submit"
                  className="keep-white px-5 py-2.5 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white font-extrabold inline-flex items-center gap-1.5 shadow-sm"
                >
                  <CheckCircle2 className="w-4 h-4" />
                  <span>{t.citizen.submitFeedbackBtn}</span>
                </button>
              </motion.form>
            )}
          </AnimatePresence>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {myRequests.map((req) => (
              <div
                key={req.id}
                className="p-5 rounded-3xl bg-white/[0.025] border border-white/[0.08] hover:border-white/20 transition flex flex-col justify-between"
              >
                <div>
                  <div className="flex flex-wrap items-center justify-between gap-2 mb-3">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="text-xs font-mono font-bold px-2.5 py-1 rounded-lg bg-white/10 text-white">
                        {req.referenceCode}
                      </span>
                      <span className="text-xs px-2.5 py-0.5 rounded-full bg-emerald-500/15 border border-emerald-500/30 text-emerald-300 font-bold">
                        {req.status}
                      </span>
                      <span className="text-xs px-2.5 py-0.5 rounded-full bg-indigo-500/15 border border-indigo-500/30 text-indigo-300 font-semibold">
                        {t.citizen.categories[req.category] || req.category}
                      </span>
                    </div>

                    <button
                      type="button"
                      onClick={() => setFeedbackRequestId(req.id)}
                      className="keep-white px-3 py-1.5 rounded-xl bg-indigo-600 hover:bg-indigo-500 text-white text-xs font-bold flex items-center gap-1.5 transition shadow-sm"
                    >
                      <MessageSquare className="w-3.5 h-3.5" />
                      <span>{t.citizen.rateDeliveryBtn}</span>
                    </button>
                  </div>

                  <p className="text-base font-extrabold text-white">{req.title}</p>
                  <p className="text-xs text-zinc-400 mt-1 leading-relaxed">{req.description}</p>
                </div>

                {req.timeline && req.timeline.length > 0 && (
                  <div className="mt-4 pt-3 border-t border-white/[0.07] space-y-1.5">
                    {req.timeline.slice(0, 2).map((item: any) => (
                      <p key={item.id} className="text-xs text-zinc-400 flex items-center gap-1.5">
                        <History className="w-3.5 h-3.5 text-indigo-400 shrink-0" />
                        <span className="font-bold text-indigo-300">
                          {item.newStatus}:
                        </span>
                        <span className="truncate">{item.actionDescription}</span>
                      </p>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
