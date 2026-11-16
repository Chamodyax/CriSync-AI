import React, { useEffect, useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import {
  AlertOctagon,
  Clock,
  PackageCheck,
  Home,
  Sparkles,
  GitMerge,
  Globe,
  CheckCircle2,
  Flame,
  Archive,
  MapPin,
  Users,
  Package,
  History,
  ArrowRight,
  ShieldAlert,
  Search,
  Radio,
  ChevronDown,
  ChevronUp,
  PlusCircle,
  Send,
  UserPlus,
  KeyRound,
  X,
  ZoomIn,
  Camera,
  BarChart3,
  AlertTriangle,
  Ban
} from 'lucide-react';
import { LANGUAGE_OPTIONS, SupportedLanguage, TRANSLATIONS } from '../i18n';
import { resolveEvidencePhoto } from '../utils/fieldPhotos';
import { TelemetrySparkline } from '../components/TelemetrySparkline';

function AnimatedCounter({ value }: { value: number }) {
  const [displayValue, setDisplayValue] = useState(0);

  useEffect(() => {
    const target = Number(value) || 0;
    const durationMs = 650;
    let frameId = 0;
    const start = performance.now();

    const step = (now: number) => {
      const progress = Math.min(1, (now - start) / durationMs);
      const eased = 1 - Math.pow(1 - progress, 3);
      setDisplayValue(Math.round(target * eased));
      if (progress < 1) {
        frameId = requestAnimationFrame(step);
      }
    };

    frameId = requestAnimationFrame(step);
    return () => cancelAnimationFrame(frameId);
  }, [value]);

  return <>{displayValue}</>;
}

interface DashboardTabProps {
  userRole: string;
  metrics: any;
  briefing: any;
  briefingLang: string;
  requests: any[];
  crises: any[];
  organisations?: any[];
  onChangeBriefingLanguage: (lang: string) => void;
  onMergeDuplicate: (req: any) => void;
  onSelectForAiGovernance: (req: any) => void;
  onUpdateRequestStatus: (id: string, newStatus: string, note: string, assignedOrganisationId?: string | null) => Promise<void>;
  onCreatePublicAlert: (payload: {
    crisisEventId: string;
    title: string;
    message: string;
    severityLevel: string;
    safetyGuidance: string;
  }) => Promise<void>;
  onCreateCrisisEvent: (payload: {
    title: string;
    crisisType: string;
    severity: string;
    country: string;
    region: string;
    description: string;
    centerLatitude: number;
    centerLongitude: number;
    radiusKm: number;
  }) => Promise<void>;
  onCreateOperationalArea?: (crisisId: string, payload: any) => Promise<void>;
  onRegisterCitizenOnBehalf?: (payload: any) => Promise<any>;
  onInviteStaff?: (payload: any) => Promise<any>;
}

export const DashboardTab: React.FC<DashboardTabProps> = ({
  userRole,
  metrics,
  briefing,
  briefingLang,
  requests,
  crises,
  organisations = [],
  onChangeBriefingLanguage,
  onMergeDuplicate,
  onSelectForAiGovernance,
  onUpdateRequestStatus,
  onCreatePublicAlert,
  onCreateCrisisEvent,
  onCreateOperationalArea,
  onRegisterCitizenOnBehalf,
  onInviteStaff
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [activeFilter, setActiveFilter] = useState<'all' | 'critical' | 'overdue' | 'suspicious' | 'duplicates' | 'verified'>('all');
  const [expandedCards, setExpandedCards] = useState<Record<string, boolean>>({});
  const canDeclareOrAlert =
    userRole === 'EmergencyCoordinator' || userRole === 'SystemAdmin' || userRole === 'OrganizationAdmin';
  const canRegisterCitizenProxy =
    userRole === 'CaseManager' || canDeclareOrAlert;
  const canInviteOperational = canDeclareOrAlert;

  const [commandOpen, setCommandOpen] = useState(crises.length === 0 || userRole === 'CaseManager');
  const [commandTab, setCommandTab] = useState<'alert' | 'crisis' | 'operational-zone' | 'citizen-behalf' | 'invite-staff'>(
    userRole === 'CaseManager' ? 'citizen-behalf' : crises.length === 0 ? 'crisis' : 'alert'
  );
  const [zoneForm, setZoneForm] = useState({
    crisisId: '',
    zoneName: '',
    district: '',
    latitude: 6.6828,
    longitude: 80.3992,
    coverageRadiusKm: 12,
    estimatedAffectedPopulation: 5000,
    riskLevel: 'High',
    assignedOrganisationId: ''
  });
  const [citizenProxyForm, setCitizenProxyForm] = useState({
    fullName: '',
    email: '',
    phoneNumber: '',
    password: '',
    primaryAddress: '',
    totalMembers: 1,
    childrenCount: 0,
    elderlyCount: 0,
    hasMobilityNeeds: false,
    hasChronicMedicalNeeds: false,
    medicalNotes: '',
    preferredLanguage: 'si'
  });
  const [inviteForm, setInviteForm] = useState({
    fullName: '',
    email: '',
    phoneNumber: '',
    temporaryPassword: '',
    role: 'CaseManager',
    organisationId: '',
    preferredLanguage: 'en'
  });
  const [lastInvitationResult, setLastInvitationResult] = useState<any | null>(null);
  const [unlockedFields, setUnlockedFields] = useState<Record<string, boolean>>({});
  const unlockField = (fieldKey: string) => {
    setUnlockedFields((prev) => (prev[fieldKey] ? prev : { ...prev, [fieldKey]: true }));
  };
  const [alertCrisisId, setAlertCrisisId] = useState(crises[0]?.id || '');
  const [alertTitle, setAlertTitle] = useState('');
  const [alertSeverity, setAlertSeverity] = useState('Critical');
  const [alertMessage, setAlertMessage] = useState('');
  const [alertGuidance, setAlertGuidance] = useState('');
  const [crisisTitle, setCrisisTitle] = useState('');
  const [crisisType, setCrisisType] = useState('Flood');
  const [crisisSeverity, setCrisisSeverity] = useState('Critical');
  const [crisisCountry, setCrisisCountry] = useState('');
  const [crisisRegion, setCrisisRegion] = useState('');
  const [crisisDescription, setCrisisDescription] = useState('');
  const [crisisLat, setCrisisLat] = useState(6.9271);
  const [crisisLon, setCrisisLon] = useState(79.8612);
  const [crisisRadiusKm, setCrisisRadiusKm] = useState(15);
  const [commandSubmitting, setCommandSubmitting] = useState(false);
  const [lightboxPhoto, setLightboxPhoto] = useState<{
    src: string;
    title: string;
    caption: string;
    fileName?: string;
  } | null>(null);

  useEffect(() => {
    if (!lightboxPhoto) return;
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setLightboxPhoto(null);
      }
    };
    window.addEventListener('keydown', handleEscape);
    return () => window.removeEventListener('keydown', handleEscape);
  }, [lightboxPhoto]);

  const activeAlertCrisisId = alertCrisisId || crises[0]?.id || '';

  const handleCitizenProxySubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!onRegisterCitizenOnBehalf) return;
    setCommandSubmitting(true);
    try {
      await onRegisterCitizenOnBehalf(citizenProxyForm);
      setCitizenProxyForm({
        fullName: '',
        email: '',
        phoneNumber: '',
        password: '',
        primaryAddress: '',
        totalMembers: 1,
        childrenCount: 0,
        elderlyCount: 0,
        hasMobilityNeeds: false,
        hasChronicMedicalNeeds: false,
        medicalNotes: '',
        preferredLanguage: 'si'
      });
    } finally {
      setCommandSubmitting(false);
    }
  };

  const handleInviteStaffSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!onInviteStaff) return;
    setCommandSubmitting(true);
    try {
      const invited = await onInviteStaff({
        fullName: inviteForm.fullName,
        email: inviteForm.email,
        phoneNumber: inviteForm.phoneNumber,
        temporaryPassword: inviteForm.temporaryPassword,
        role: inviteForm.role,
        organisationId: inviteForm.organisationId || null,
        preferredLanguage: inviteForm.preferredLanguage
      });
      setLastInvitationResult({
        ...invited,
        temporaryPassword: inviteForm.temporaryPassword
      });
      setInviteForm({
        fullName: '',
        email: '',
        phoneNumber: '',
        temporaryPassword: '',
        role: 'CaseManager',
        organisationId: '',
        preferredLanguage: 'en'
      });
    } finally {
      setCommandSubmitting(false);
    }
  };

  const handleBroadcastAlert = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!activeAlertCrisisId) return;
    setCommandSubmitting(true);
    await onCreatePublicAlert({
      crisisEventId: activeAlertCrisisId,
      title: alertTitle,
      message: alertMessage,
      severityLevel: alertSeverity,
      safetyGuidance: alertGuidance
    });
    setAlertTitle('');
    setAlertMessage('');
    setAlertGuidance('');
    setCommandSubmitting(false);
  };

  const handleRegisterCrisis = async (e: React.FormEvent) => {
    e.preventDefault();
    setCommandSubmitting(true);
    await onCreateCrisisEvent({
      title: crisisTitle,
      crisisType,
      severity: crisisSeverity,
      country: crisisCountry,
      region: crisisRegion,
      description: crisisDescription,
      centerLatitude: Number(crisisLat),
      centerLongitude: Number(crisisLon),
      radiusKm: Number(crisisRadiusKm)
    });
    setCrisisTitle('');
    setCrisisCountry('');
    setCrisisRegion('');
    setCrisisDescription('');
    setCommandTab('alert');
    setCommandSubmitting(false);
  };

  const handleCreateZoneSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const targetCrisis = zoneForm.crisisId || crises[0]?.id;
    if (!onCreateOperationalArea || !targetCrisis) return;
    setCommandSubmitting(true);
    try {
      await onCreateOperationalArea(targetCrisis, {
        zoneName: zoneForm.zoneName,
        district: zoneForm.district,
        latitude: Number(zoneForm.latitude),
        longitude: Number(zoneForm.longitude),
        coverageRadiusKm: Number(zoneForm.coverageRadiusKm),
        estimatedAffectedPopulation: Number(zoneForm.estimatedAffectedPopulation),
        riskLevel: zoneForm.riskLevel,
        assignedOrganisationId: zoneForm.assignedOrganisationId || null
      });
      setZoneForm({
        ...zoneForm,
        zoneName: '',
        district: ''
      });
    } finally {
      setCommandSubmitting(false);
    }
  };

  const langKey = (briefingLang as SupportedLanguage) || 'en';
  const t = TRANSLATIONS[langKey] || TRANSLATIONS.en;

  const toggleExpandCard = (id: string) => {
    setExpandedCards((prev) => ({ ...prev, [id]: !prev[id] }));
  };

  const canManageCases =
    userRole === 'EmergencyCoordinator' ||
    userRole === 'CaseManager' ||
    userRole === 'OrganizationAdmin' ||
    userRole === 'SystemAdmin';

  const canReviewAi =
    userRole === 'EmergencyCoordinator' ||
    userRole === 'CaseManager' ||
    userRole === 'OrganizationAdmin' ||
    userRole === 'SystemAdmin';

  const isRequestOverdue = (req: any) => {
    const isTerminal =
      req.status === 'Fulfilled' ||
      req.status === 'Closed' ||
      req.status === 'MergedAsDuplicate' ||
      req.status === 'Rejected';
    if (isTerminal || !req.createdAtUtc) return false;
    const ageHours = (Date.now() - new Date(req.createdAtUtc).getTime()) / 3600000;
    return ageHours >= 4;
  };

  const criticalCount = requests.filter(
    (r) => r.finalApprovedUrgency === 'LifeThreatening' || r.finalApprovedUrgency === 'Critical'
  ).length;
  const overdueCount = requests.filter((r) => isRequestOverdue(r)).length;
  const suspiciousCount = requests.filter((r) => Boolean(r.isFlaggedSuspicious)).length;
  const duplicateCount = requests.filter((r) => Boolean(r.isFlaggedPotentialDuplicate)).length;

  const filteredRequests = requests.filter((req) => {
    const matchesSearch =
      !searchQuery.trim() ||
      req.referenceCode?.toLowerCase().includes(searchQuery.toLowerCase()) ||
      req.title?.toLowerCase().includes(searchQuery.toLowerCase()) ||
      req.category?.toLowerCase().includes(searchQuery.toLowerCase()) ||
      req.locationAddress?.toLowerCase().includes(searchQuery.toLowerCase());

    if (!matchesSearch) return false;

    if (activeFilter === 'critical') {
      return req.finalApprovedUrgency === 'LifeThreatening' || req.finalApprovedUrgency === 'Critical';
    }
    if (activeFilter === 'overdue') {
      return isRequestOverdue(req);
    }
    if (activeFilter === 'suspicious') {
      return Boolean(req.isFlaggedSuspicious);
    }
    if (activeFilter === 'duplicates') {
      return Boolean(req.isFlaggedPotentialDuplicate);
    }
    if (activeFilter === 'verified') {
      return req.status === 'Verified' || req.status === 'Escalated' || req.status === 'Dispatched';
    }
    return true;
  });

  const activeLangOption = LANGUAGE_OPTIONS.find((l) => l.code === langKey) || LANGUAGE_OPTIONS[0];

  return (
    <div className="space-y-6" dir={t.dir}>
      {crises.length === 0 && requests.length === 0 && (
        <div className="bento-glass rounded-3xl p-5 border border-indigo-500/35 bg-indigo-500/[0.06] flex flex-wrap items-start justify-between gap-4">
          <div className="space-y-1.5 max-w-3xl">
            <span className="inline-flex items-center gap-1.5 text-[11px] font-bold uppercase tracking-wider text-indigo-300">
              <Sparkles className="w-3.5 h-3.5" />
              Getting Started — Clean Project Setup Guide
            </span>
            <h2 className="text-base sm:text-lg font-extrabold text-white">
              Welcome to CriSync Live Mode (All Demo Data Removed)
            </h2>
            <p className="text-xs text-zinc-300 leading-relaxed">
              1) As <strong className="text-white">Emergency Coordinator</strong> or <strong className="text-white">System Admin</strong>, click <strong className="text-indigo-300">Declare New Crisis Event</strong> below to open an active emergency. 2) Broadcast a <strong className="text-rose-300">Public Alert</strong> and add safe <strong className="text-amber-300">Shelters</strong> & <strong className="text-emerald-300">Warehouse Stock</strong>. 3) Register or sign in as a <strong className="text-sky-300">Citizen</strong> to submit real help requests for AI triage and dispatch.
            </p>
          </div>
        </div>
      )}

      {(canDeclareOrAlert || canRegisterCitizenProxy || canInviteOperational) && (
        <div className="bento-glass rounded-3xl overflow-hidden border border-indigo-500/30">
          <button
            type="button"
            onClick={() => setCommandOpen((prev) => !prev)}
            className="w-full px-5 py-4 flex flex-wrap items-center justify-between gap-3 text-left hover:bg-white/[0.03] transition"
          >
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-2xl bg-indigo-500/20 border border-indigo-500/40 text-indigo-300 flex items-center justify-center shrink-0">
                <Radio className="w-5 h-5" />
              </div>
              <div>
                <span className="text-xs font-bold uppercase tracking-wider text-indigo-400">
                  Command, Invitations & Field Onboarding Actions
                </span>
                <h2 className="text-sm sm:text-base font-extrabold text-white">
                  {userRole === 'CaseManager'
                    ? 'Register Offline Citizens on Behalf of Victims'
                    : 'Broadcast Alerts, Declare Crises, Register Citizens & Invite Operational Staff'}
                </h2>
              </div>
            </div>
            <span className="inline-flex items-center gap-1.5 text-xs font-bold px-3 py-1.5 rounded-xl bg-white/[0.05] border border-white/15 text-zinc-200">
              <span>{commandOpen ? 'Hide Command Panel' : 'Open Command Panel'}</span>
              {commandOpen ? <ChevronUp className="w-4 h-4" /> : <ChevronDown className="w-4 h-4" />}
            </span>
          </button>

          <AnimatePresence>
            {commandOpen && (
              <motion.div
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: 'auto' }}
                exit={{ opacity: 0, height: 0 }}
                transition={{ duration: 0.25, ease: 'easeOut' }}
                className="overflow-hidden"
              >
                <div className="px-5 pb-5 pt-2 border-t border-white/[0.08] space-y-4">
              <div className="flex flex-wrap gap-2">
                {canDeclareOrAlert && (
                  <>
                    <button
                      type="button"
                      onClick={() => setCommandTab('alert')}
                      className={`px-4 py-2 rounded-xl text-xs font-extrabold border transition flex items-center gap-1.5 ${
                        commandTab === 'alert'
                          ? 'bg-rose-600 border-rose-500 text-white keep-white shadow-sm'
                          : 'bg-white/[0.03] border-white/10 text-zinc-300 hover:text-white'
                      }`}
                    >
                      <ShieldAlert className="w-3.5 h-3.5" />
                      <span>Broadcast Public Alert</span>
                    </button>
                    <button
                      type="button"
                      onClick={() => setCommandTab('crisis')}
                      className={`px-4 py-2 rounded-xl text-xs font-extrabold border transition flex items-center gap-1.5 ${
                        commandTab === 'crisis'
                          ? 'bg-indigo-600 border-indigo-500 text-white keep-white shadow-sm'
                          : 'bg-white/[0.03] border-white/10 text-zinc-300 hover:text-white'
                      }`}
                    >
                      <PlusCircle className="w-3.5 h-3.5" />
                      <span>Declare New Crisis Event</span>
                    </button>
                    {onCreateOperationalArea && crises.length > 0 && (
                      <button
                        type="button"
                        onClick={() => setCommandTab('operational-zone')}
                        className={`px-4 py-2 rounded-xl text-xs font-extrabold border transition flex items-center gap-1.5 ${
                          commandTab === 'operational-zone'
                            ? 'bg-cyan-600 border-cyan-500 text-white keep-white shadow-sm'
                            : 'bg-white/[0.03] border-white/10 text-zinc-300 hover:text-white'
                        }`}
                      >
                        <MapPin className="w-3.5 h-3.5" />
                        <span>Add Operational Area</span>
                      </button>
                    )}
  );
};
