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
  Bot,
  ArrowRight,
  ArrowLeft,
  Compass,
  LayoutGrid,
  Lightbulb,
  ClipboardCheck,
  AlertCircle,
  Camera,
  BellRing,
  Wifi,
  WifiOff,
  RefreshCw
} from 'lucide-react';
import {
  LANGUAGE_OPTIONS,
  SupportedLanguage,
  TRANSLATIONS,
  QuickTemplateItem,
  generateAutomatedAiSupportReply
} from '../i18n';

interface QueuedOfflineRequest {
  localId: string;
  queuedAtIso: string;
  payload: any;
}

const OFFLINE_QUEUE_STORAGE_KEY = 'crisync_offline_request_queue';

interface CitizenPortalTabProps {
  alerts: any[];
  crises?: any[];
  shelters: any[];
  requests: any[];
  notifications?: any[];
  household: any | null;
  currentUserId?: string;
  briefing?: any | null;
  activeLanguage?: SupportedLanguage;
  onChangeLanguage?: (lang: SupportedLanguage) => void;
  onUploadEvidence: (file: File) => Promise<string | null>;
  onSubmitRequest: (payload: any) => Promise<boolean | void>;
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

type WizardStepNumber = 1 | 2 | 3 | 4 | 5;

export const CitizenPortalTab: React.FC<CitizenPortalTabProps> = ({
  alerts,
  crises = [],
  shelters,
  requests,
  notifications = [],
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
  const [portalMode, setPortalMode] = useState<'guided' | 'tabs'>('guided');
  const [wizardStep, setWizardStep] = useState<WizardStepNumber>(1);
  const [familyConfirmed, setFamilyConfirmed] = useState(false);
  const [stepError, setStepError] = useState<string | null>(null);
  const [savingFamily, setSavingFamily] = useState(false);
  const [submittingRequest, setSubmittingRequest] = useState(false);
  const [aiHelperOpen, setAiHelperOpen] = useState(false);
  const [appliedTemplateLabel, setAppliedTemplateLabel] = useState<string | null>(null);
  const [lastSubmittedTitle, setLastSubmittedTitle] = useState('');

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
  const [isOnline, setIsOnline] = useState<boolean>(
    typeof navigator !== 'undefined' ? navigator.onLine : true
  );
  const [queuedOfflineRequests, setQueuedOfflineRequests] = useState<QueuedOfflineRequest[]>(() => {
    try {
      const raw = localStorage.getItem(OFFLINE_QUEUE_STORAGE_KEY);
      if (!raw) return [];
      const parsed = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  });
  const [syncingOfflineQueue, setSyncingOfflineQueue] = useState(false);
  const [offlineStatusNotice, setOfflineStatusNotice] = useState<string | null>(null);
  const [lastSubmitQueuedOffline, setLastSubmitQueuedOffline] = useState(false);

  useEffect(() => {
    try {
      localStorage.setItem(OFFLINE_QUEUE_STORAGE_KEY, JSON.stringify(queuedOfflineRequests));
    } catch {
    }
  }, [queuedOfflineRequests]);

  const syncOfflineQueue = async () => {
    if (queuedOfflineRequests.length === 0 || syncingOfflineQueue) return;
    if (typeof navigator !== 'undefined' && !navigator.onLine) return;

    setSyncingOfflineQueue(true);
    const remaining: QueuedOfflineRequest[] = [];
    let syncedCount = 0;

    for (const item of queuedOfflineRequests) {
      try {
        const result = await onSubmitRequest(item.payload);
        if (result === false) {
          remaining.push(item);
        } else {
          syncedCount++;
        }
      } catch {
        remaining.push(item);
      }
    }

    setQueuedOfflineRequests(remaining);
    setSyncingOfflineQueue(false);
    if (syncedCount > 0) {
      setOfflineStatusNotice(
        remaining.length === 0
          ? `Synced ${syncedCount} offline request(s) to the coordinator queue.`
          : `Synced ${syncedCount} offline request(s); ${remaining.length} waiting for retry.`
      );
    }
  };

  useEffect(() => {
    const handleOnline = () => {
      setIsOnline(true);
      void syncOfflineQueue();
    };
    const handleOffline = () => {
      setIsOnline(false);
      setOfflineStatusNotice('Connection offline. New requests will be stored safely on this device and auto-synced when online.');
    };

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);
    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
    };
  }, [queuedOfflineRequests]);

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
  const selectedCrisisObj = crises.find((c) => c.id === activeCrisisId) || crises[0] || null;

  const calculateWizardCompletion = (): number => {
    if (wizardStep === 5) return 100;
    let pct = 0;

    if (wizardStep > 1 || familyConfirmed) {
      pct += 25;
    } else if (headName.trim().length > 0) {
      pct += 15;
    }

    if (wizardStep > 2) {
      pct += 25;
    } else if (wizardStep === 2 && appliedTemplateLabel) {
      pct += 15;
    }

    if (wizardStep > 3) {
      pct += 35;
    } else if (wizardStep === 3) {
      if (title.trim().length >= 3) pct += 12;
      if (description.trim().length >= 5) pct += 12;
      if (address.trim().length >= 3) pct += 11;
    }

    if (wizardStep === 4) {
      pct += 5;
    }

    return Math.min(95, pct);
  };

  const completionPercent = calculateWizardCompletion();

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
    setAppliedTemplateLabel(tpl.label);
    setStepError(null);
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (!selectedFile) return;
    const safeName = await onUploadEvidence(selectedFile);
    if (safeName) {
      setEvidenceFileName(safeName);
    }
  };

  const buildRequestPayload = () => ({
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

  const buildHouseholdPayload = () => ({
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

  const submitOrQueueRequest = async (payload: any): Promise<'sent' | 'queued'> => {
    const currentlyOffline = typeof navigator !== 'undefined' && !navigator.onLine;
    if (!currentlyOffline) {
      try {
        const ok = await onSubmitRequest(payload);
        if (ok !== false) {
          setLastSubmitQueuedOffline(false);
          return 'sent';
        }
      } catch {
      }
    }

    const queuedEntry: QueuedOfflineRequest = {
      localId: `OFFLINE-${Date.now()}`,
      queuedAtIso: new Date().toISOString(),
      payload
    };
    setQueuedOfflineRequests((prev) => [queuedEntry, ...prev]);
    setLastSubmitQueuedOffline(true);
    setOfflineStatusNotice(
      'Request captured in Offline Outbox on this device and queued for automatic upload once connectivity restores.'
    );
    return 'queued';
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const submittedHeadline = title;
    await submitOrQueueRequest(buildRequestPayload());
    setLastSubmittedTitle(submittedHeadline);
    setTitle('');
    setDescription('');
    setEvidenceFileName('');
    setAppliedTemplateLabel(null);
  };

  const handleHouseholdSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSavingFamily(true);
    try {
      await onSaveHousehold(buildHouseholdPayload());
      setFamilyConfirmed(true);
    } finally {
      setSavingFamily(false);
    }
  };

  const handleStep1SaveAndNext = async () => {
    setStepError(null);
    if (headName.trim().length > 0) {
      setSavingFamily(true);
      try {
        await onSaveHousehold(buildHouseholdPayload());
        setFamilyConfirmed(true);
      } finally {
        setSavingFamily(false);
      }
    }
    setWizardStep(2);
  };

  const handleStep2Next = () => {
    if (!activeCrisisId && crises.length === 0) {
      setStepError('No active crisis event is open yet. A Coordinator must declare one before requests can be submitted, or switch to Safe Shelters & AI Help.');
      return;
    }
    if (!category || !urgency) {
      setStepError('Please select a help category and urgency level before continuing.');
      return;
    }
    setStepError(null);
    setWizardStep(3);
  };

  const handleStep3Next = () => {
    const missing: string[] = [];
    if (!title.trim()) missing.push('Short Emergency Headline');
    if (!description.trim()) missing.push('Situation Description');
    if (!address.trim()) missing.push('Location Address or Landmark');

    if (missing.length > 0) {
      setStepError(`Please fill in before reviewing: ${missing.join(', ')}.`);
      return;
    }
    setStepError(null);
    setWizardStep(4);
  };

  const handleWizardFinalSubmit = async () => {
    if (!activeCrisisId && (typeof navigator === 'undefined' || navigator.onLine)) {
      setStepError('An active emergency operation is required before submitting.');
      return;
    }
    setStepError(null);
    setSubmittingRequest(true);
    try {
      const submittedHeadline = title;
      await submitOrQueueRequest(buildRequestPayload());
      setLastSubmittedTitle(submittedHeadline);
      setTitle('');
      setDescription('');
      setEvidenceFileName('');
      setAppliedTemplateLabel(null);
      setWizardStep(5);
    } finally {
      setSubmittingRequest(false);
    }
  };

  const handleStartNewRequest = () => {
    setStepError(null);
    setAppliedTemplateLabel(null);
    setWizardStep(2);
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

  const wizardStepsMeta: { num: 1 | 2 | 3 | 4; label: string; sub: string }[] = [
    { num: 1, label: 'Family Info', sub: 'Household & medical needs' },
    { num: 2, label: 'Help Type', sub: 'Category, urgency & preset' },
    { num: 3, label: 'Details & Photo', sub: 'Address, count & evidence' },
    { num: 4, label: 'Review & Send', sub: 'Confirm & track aid' }
  );
};
