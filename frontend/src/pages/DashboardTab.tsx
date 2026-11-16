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
  KeyRound
} from 'lucide-react';
import { LANGUAGE_OPTIONS, SupportedLanguage, TRANSLATIONS } from '../i18n';

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
  onUpdateRequestStatus: (id: string, newStatus: string, note: string) => Promise<void>;
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
  onRegisterCitizenOnBehalf,
  onInviteStaff
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [activeFilter, setActiveFilter] = useState<'all' | 'critical' | 'duplicates' | 'verified'>('all');
  const [expandedCards, setExpandedCards] = useState<Record<string, boolean>>({});
  const canDeclareOrAlert =
    userRole === 'EmergencyCoordinator' || userRole === 'SystemAdmin' || userRole === 'OrganizationAdmin';
  const canRegisterCitizenProxy =
    userRole === 'CaseManager' || canDeclareOrAlert;
  const canInviteOperational = canDeclareOrAlert;

  const [commandOpen, setCommandOpen] = useState(crises.length === 0 || userRole === 'CaseManager');
  const [commandTab, setCommandTab] = useState<'alert' | 'crisis' | 'citizen-behalf' | 'invite-staff'>(
    userRole === 'CaseManager' ? 'citizen-behalf' : crises.length === 0 ? 'crisis' : 'alert'
  );
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

  const criticalCount = requests.filter(
    (r) => r.finalApprovedUrgency === 'LifeThreatening' || r.finalApprovedUrgency === 'Critical'
  ).length;

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
                  </>
                )}
                {canRegisterCitizenProxy && onRegisterCitizenOnBehalf && (
                  <button
                    type="button"
                    onClick={() => setCommandTab('citizen-behalf')}
                    className={`px-4 py-2 rounded-xl text-xs font-extrabold border transition flex items-center gap-1.5 ${
                      commandTab === 'citizen-behalf'
                        ? 'bg-sky-600 border-sky-500 text-white keep-white shadow-sm'
                        : 'bg-white/[0.03] border-white/10 text-zinc-300 hover:text-white'
                    }`}
                  >
                    <UserPlus className="w-3.5 h-3.5" />
                    <span>Register Citizen on Behalf</span>
                  </button>
                )}
                {canInviteOperational && onInviteStaff && (
                  <button
                    type="button"
                    onClick={() => setCommandTab('invite-staff')}
                    className={`px-4 py-2 rounded-xl text-xs font-extrabold border transition flex items-center gap-1.5 ${
                      commandTab === 'invite-staff'
                        ? 'bg-amber-600 border-amber-500 text-white keep-white shadow-sm'
                        : 'bg-white/[0.03] border-white/10 text-zinc-300 hover:text-white'
                    }`}
                  >
                    <KeyRound className="w-3.5 h-3.5" />
                    <span>Invite Operational Role</span>
                  </button>
                )}
              </div>

              {commandTab === 'citizen-behalf' && canRegisterCitizenProxy ? (
                <form key="citizen-behalf-form" onSubmit={handleCitizenProxySubmit} className="grid grid-cols-1 md:grid-cols-3 gap-3.5 text-xs" autoComplete="off">
                  <div
                    aria-hidden="true"
                    className="pointer-events-none fixed -left-[9999px] top-0 h-px w-px opacity-0 overflow-hidden"
                  >
                    <input type="text" name="prevent_proxy_autofill_user" autoComplete="username" tabIndex={-1} defaultValue="" />
                    <input type="password" name="prevent_proxy_autofill_pass" autoComplete="current-password" tabIndex={-1} defaultValue="" />
                  </div>
                  <div className="md:col-span-3 p-3 rounded-2xl bg-sky-500/10 border border-sky-500/30 text-sky-200 text-[11px]">
                    <strong className="text-white">Case Manager Proxy Onboarding:</strong> Register an affected Citizen who lacks a phone or internet connection during a disaster, including their household vulnerability profile.
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Citizen Full Name</label>
                    <input
                      type="text"
                      name="crisync_proxy_citizen_name"
                      autoComplete="off"
                      required
                      value={citizenProxyForm.fullName}
                      onChange={(e) => setCitizenProxyForm({ ...citizenProxyForm, fullName: e.target.value })}
                      placeholder="Affected resident full name"
                      className="rsvp-input"
                    />
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Email or Household ID</label>
                    <input
                      type="email"
                      name="crisync_proxy_citizen_email"
                      autoComplete="off"
                      required
                      readOnly={!unlockedFields.citizenEmail}
                      onFocus={() => unlockField('citizenEmail')}
                      onMouseDown={() => unlockField('citizenEmail')}
                      onKeyDown={() => unlockField('citizenEmail')}
                      value={citizenProxyForm.email}
                      onChange={(e) => setCitizenProxyForm({ ...citizenProxyForm, email: e.target.value })}
                      placeholder="citizen.name@offline.crisync.org"
                      className="rsvp-input"
                    />
                    <p className="text-[11px] text-zinc-400/75 opacity-75 mt-1 font-normal">
                      Example: kamal.perera@offline.crisync.org (if resident has no email)
                    </p>
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Contact Phone / Neighbour Phone</label>
                    <input
                      type="text"
                      name="crisync_proxy_citizen_phone"
                      autoComplete="off"
                      value={citizenProxyForm.phoneNumber}
                      onChange={(e) => setCitizenProxyForm({ ...citizenProxyForm, phoneNumber: e.target.value })}
                      placeholder="Optional contact number"
                      className="rsvp-input"
                    />
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Assigned Access Password</label>
                    <input
                      type="text"
                      name="crisync_proxy_citizen_password"
                      autoComplete="new-password"
                      required
                      minLength={6}
                      readOnly={!unlockedFields.citizenPassword}
                      onFocus={() => unlockField('citizenPassword')}
                      onMouseDown={() => unlockField('citizenPassword')}
                      onKeyDown={() => unlockField('citizenPassword')}
                      value={citizenProxyForm.password}
                      onChange={(e) => setCitizenProxyForm({ ...citizenProxyForm, password: e.target.value })}
                      placeholder="Temporary access password"
                      className="rsvp-input font-mono"
                    />
                  </div>
                  <div className="md:col-span-2">
                    <label className="block font-bold text-zinc-300 mb-1">Residential Location / Relief Zone</label>
                    <input
                      type="text"
                      required
                      value={citizenProxyForm.primaryAddress}
                      onChange={(e) => setCitizenProxyForm({ ...citizenProxyForm, primaryAddress: e.target.value })}
                      placeholder="Village, street, or shelter camp name"
                      className="rsvp-input"
                    />
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Total Family Members</label>
                    <input
                      type="number"
                      min={1}
                      value={citizenProxyForm.totalMembers}
                      onChange={(e) => setCitizenProxyForm({ ...citizenProxyForm, totalMembers: Number(e.target.value) })}
                      className="rsvp-input font-mono"
                    />
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Children Count</label>
                    <input
                      type="number"
                      min={0}
                      value={citizenProxyForm.childrenCount}
                      onChange={(e) => setCitizenProxyForm({ ...citizenProxyForm, childrenCount: Number(e.target.value) })}
                      className="rsvp-input font-mono"
                    />
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Elderly Count</label>
                    <input
                      type="number"
                      min={0}
                      value={citizenProxyForm.elderlyCount}
                      onChange={(e) => setCitizenProxyForm({ ...citizenProxyForm, elderlyCount: Number(e.target.value) })}
                      className="rsvp-input font-mono"
                    />
                  </div>
                  <div className="md:col-span-3 flex flex-wrap items-center gap-4">
                    <label className="flex items-center gap-2 text-zinc-200 font-semibold cursor-pointer">
                      <input
                        type="checkbox"
                        checked={citizenProxyForm.hasMobilityNeeds}
                        onChange={(e) => setCitizenProxyForm({ ...citizenProxyForm, hasMobilityNeeds: e.target.checked })}
                        className="w-4 h-4 rounded"
                      />
                      Mobility / Wheelchair Support Needed
                    </label>
                    <label className="flex items-center gap-2 text-zinc-200 font-semibold cursor-pointer">
                      <input
                        type="checkbox"
                        checked={citizenProxyForm.hasChronicMedicalNeeds}
                        onChange={(e) => setCitizenProxyForm({ ...citizenProxyForm, hasChronicMedicalNeeds: e.target.checked })}
                        className="w-4 h-4 rounded"
                      />
                      Chronic Medical / Medication Needs
                    </label>
                  </div>
                  <div className="md:col-span-3 flex justify-end">
                    <button
                      type="submit"
                      disabled={commandSubmitting}
                      className="keep-white px-5 py-2.5 rounded-2xl bg-sky-600 hover:bg-sky-500 text-white font-extrabold flex items-center gap-2 shadow-sm disabled:opacity-50"
                    >
                      <UserPlus className="w-4 h-4" />
                      <span>{commandSubmitting ? 'Registering...' : 'Register Citizen Account & Family Profile'}</span>
                    </button>
                  </div>
                </form>
              ) : commandTab === 'invite-staff' && canInviteOperational ? (
                <form key="invite-staff-form" onSubmit={handleInviteStaffSubmit} className="grid grid-cols-1 md:grid-cols-3 gap-3.5 text-xs" autoComplete="off">
                  <div
                    aria-hidden="true"
                    className="pointer-events-none fixed -left-[9999px] top-0 h-px w-px opacity-0 overflow-hidden"
                  >
                    <input type="text" name="prevent_invite_autofill_user" autoComplete="username" tabIndex={-1} defaultValue="" />
                    <input type="password" name="prevent_invite_autofill_pass" autoComplete="current-password" tabIndex={-1} defaultValue="" />
                  </div>
                  <div className="md:col-span-3 p-3 rounded-2xl bg-amber-500/10 border border-amber-500/30 text-amber-200 text-[11px]">
                    <strong className="text-white">Invitation Authority:</strong> System Admin and Emergency Coordinator can invite <strong>Case Manager</strong>, <strong>Shelter Admin</strong>, <strong>Resource & Logistics Manager</strong>, and <strong>Volunteer Coordinator</strong>. Note: <strong>Donor / Funding Officer</strong> handles money & financial transactions and can only be invited exclusively by the <strong>Global System Admin</strong>.
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Invited Role</label>
                    <select
                      value={inviteForm.role}
                      onChange={(e) => setInviteForm({ ...inviteForm, role: e.target.value })}
                      className="rsvp-input"
                    >
                      <option value="CaseManager">Case Manager</option>
                      <option value="ShelterAdmin">Shelter Administrator</option>
                      <option value="LogisticsManager">Resource & Logistics Manager</option>
                      <option value="VolunteerCoordinator">Volunteer Coordinator</option>
                      {userRole === 'SystemAdmin' && (
                        <option value="DonorOfficer">Donor / Funding Officer (Global System Admin Only)</option>
                      )}
                    </select>
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Full Name</label>
                    <input
                      type="text"
                      name="crisync_invite_staff_name"
                      autoComplete="off"
                      required
                      value={inviteForm.fullName}
                      onChange={(e) => setInviteForm({ ...inviteForm, fullName: e.target.value })}
                      placeholder="Officer's full name"
                      className="rsvp-input"
                    />
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Work Email</label>
                    <input
                      type="email"
                      name="crisync_invite_staff_email"
                      autoComplete="off"
                      required
                      readOnly={!unlockedFields.inviteEmail}
                      onFocus={() => unlockField('inviteEmail')}
                      onMouseDown={() => unlockField('inviteEmail')}
                      onKeyDown={() => unlockField('inviteEmail')}
                      value={inviteForm.email}
                      onChange={(e) => setInviteForm({ ...inviteForm, email: e.target.value })}
                      placeholder="officer@organisation.org"
                      className="rsvp-input"
                    />
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Phone Number</label>
                    <input
                      type="text"
                      name="crisync_invite_staff_phone"
                      autoComplete="off"
                      value={inviteForm.phoneNumber}
                      onChange={(e) => setInviteForm({ ...inviteForm, phoneNumber: e.target.value })}
                      placeholder="+94 77 123 4567"
                      className="rsvp-input"
                    />
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Temporary Password</label>
                    <input
                      type="text"
                      name="crisync_invite_staff_password"
                      autoComplete="new-password"
                      required
                      minLength={6}
                      readOnly={!unlockedFields.invitePassword}
                      onFocus={() => unlockField('invitePassword')}
                      onMouseDown={() => unlockField('invitePassword')}
                      onKeyDown={() => unlockField('invitePassword')}
                      value={inviteForm.temporaryPassword}
                      onChange={(e) => setInviteForm({ ...inviteForm, temporaryPassword: e.target.value })}
                      placeholder="Minimum 6 characters"
                      className="rsvp-input font-mono"
                    />
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Organisation</label>
                    <select
                      value={inviteForm.organisationId}
                      onChange={(e) => setInviteForm({ ...inviteForm, organisationId: e.target.value })}
                      className="rsvp-input"
                    >
                      <option value="">Independent / Coordinating Team</option>
                      {organisations.map((org) => (
                        <option key={org.id} value={org.id}>
                          {org.name}
                        </option>
                      ))}
                    </select>
                  </div>
                  {lastInvitationResult && (
                    <div className="md:col-span-3 p-3 rounded-2xl bg-emerald-500/15 border border-emerald-500/35 text-emerald-200 font-mono text-[11px]">
                      Invitation Issued ({lastInvitationResult.invitationCode}) • {lastInvitationResult.fullName} ({lastInvitationResult.role}) • Email: {lastInvitationResult.email} • Password: {lastInvitationResult.temporaryPassword}
                    </div>
                  )}
                  <div className="md:col-span-3 flex justify-end">
                    <button
                      type="submit"
                      disabled={commandSubmitting}
                      className="keep-white px-5 py-2.5 rounded-2xl bg-amber-600 hover:bg-amber-500 text-white font-extrabold flex items-center gap-2 shadow-sm disabled:opacity-50"
                    >
                      <KeyRound className="w-4 h-4" />
                      <span>{commandSubmitting ? 'Sending Invite...' : 'Issue Staff Invitation & Credentials'}</span>
                    </button>
                  </div>
                </form>
              ) : commandTab === 'alert' ? (
                <form onSubmit={handleBroadcastAlert} className="grid grid-cols-1 md:grid-cols-2 gap-3.5 text-xs">
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Target Crisis Event</label>
                    <select
                      value={activeAlertCrisisId}
                      onChange={(e) => setAlertCrisisId(e.target.value)}
                      className="rsvp-input"
                    >
                      {crises.length === 0 && (
                        <option value="">No Crisis Events Yet — Declare a Crisis First</option>
                      )}
                      {crises.map((c) => (
                        <option key={c.id} value={c.id}>
                          {c.title} ({c.severity})
                        </option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Alert Severity Level</label>
                    <select
                      value={alertSeverity}
                      onChange={(e) => setAlertSeverity(e.target.value)}
                      className="rsvp-input"
                    >
                      <option value="Advisory">Advisory (Stay Informed)</option>
                      <option value="Warning">Warning (Prepare to Evacuate)</option>
                      <option value="Critical">Critical (Immediate Evacuation)</option>
                    </select>
                  </div>
                  <div className="md:col-span-2">
                    <label className="block font-bold text-zinc-300 mb-1">Public Alert Headline</label>
                    <input
                      type="text"
                      required
                      value={alertTitle}
                      onChange={(e) => setAlertTitle(e.target.value)}
                      placeholder="Short clear public emergency headline"
                      className="rsvp-input"
                    />
                    <p className="text-[11px] text-zinc-400/75 opacity-75 mt-1 font-normal">
                      Example: Flash Flood Evacuation Warning — Low-Lying Areas Near Colombo Port
                    </p>
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Broadcast Message</label>
                    <textarea
                      required
                      rows={3}
                      value={alertMessage}
                      onChange={(e) => setAlertMessage(e.target.value)}
                      placeholder="Describe the danger zone and immediate public situation"
                      className="rsvp-input resize-none"
                    />
                    <p className="text-[11px] text-zinc-400/75 opacity-75 mt-1 font-normal">
                      Example: River levels exceeded danger mark at 14:30. Residents in Zone A should move to higher ground.
                    </p>
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Safety Guidance for Citizens</label>
                    <textarea
                      required
                      rows={3}
                      value={alertGuidance}
                      onChange={(e) => setAlertGuidance(e.target.value)}
                      placeholder="Actionable checklist for families and caregivers"
                      className="rsvp-input resize-none"
                    />
                    <p className="text-[11px] text-zinc-400/75 opacity-75 mt-1 font-normal">
                      Example: Switch off main breaker, pack medicines and IDs, proceed to nearest designated school shelter.
                    </p>
                  </div>
                  <div className="md:col-span-2 flex justify-end">
                    <motion.button
                      whileTap={{ scale: 0.98 }}
                      type="submit"
                      disabled={commandSubmitting || !activeAlertCrisisId}
                      className="keep-white px-5 py-2.5 rounded-2xl bg-rose-600 hover:bg-rose-500 text-white font-extrabold flex items-center gap-2 shadow-sm disabled:opacity-50"
                    >
                      <Send className="w-4 h-4" />
                      <span>{commandSubmitting ? 'Broadcasting...' : 'Broadcast Public Alert Now'}</span>
                    </motion.button>
                  </div>
                </form>
              ) : (
                <form onSubmit={handleRegisterCrisis} className="grid grid-cols-1 md:grid-cols-3 gap-3.5 text-xs">
                  <div className="md:col-span-2">
                    <label className="block font-bold text-zinc-300 mb-1">Crisis Title</label>
                    <input
                      type="text"
                      required
                      value={crisisTitle}
                      onChange={(e) => setCrisisTitle(e.target.value)}
                      placeholder="Official disaster or emergency operation name"
                      className="rsvp-input"
                    />
                    <p className="text-[11px] text-zinc-400/75 opacity-75 mt-1 font-normal">
                      Example: Western Province Monsoon Flood Response 2026
                    </p>
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Crisis Type</label>
                    <select
                      value={crisisType}
                      onChange={(e) => setCrisisType(e.target.value)}
                      className="rsvp-input"
                    >
                      <option value="Flood">Flood</option>
                      <option value="Landslide">Landslide</option>
                      <option value="Earthquake">Earthquake</option>
                      <option value="SevereStorm">Severe Storm</option>
                      <option value="DiseaseOutbreak">Disease Outbreak</option>
                      <option value="MassDisplacement">Mass Displacement</option>
                      <option value="Other">Other Emergency</option>
                    </select>
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Severity Rating</label>
                    <select
                      value={crisisSeverity}
                      onChange={(e) => setCrisisSeverity(e.target.value)}
                      className="rsvp-input"
                    >
                      <option value="Low">Low</option>
                      <option value="Moderate">Moderate</option>
                      <option value="High">High</option>
                      <option value="Critical">Critical</option>
                      <option value="Catastrophic">Catastrophic</option>
                    </select>
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Country</label>
                    <input
                      type="text"
                      required
                      value={crisisCountry}
                      onChange={(e) => setCrisisCountry(e.target.value)}
                      placeholder="Country name"
                      className="rsvp-input"
                    />
                    <p className="text-[11px] text-zinc-400/75 opacity-75 mt-1 font-normal">
                      Example: Sri Lanka
                    </p>
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Affected Region / Districts</label>
                    <input
                      type="text"
                      required
                      value={crisisRegion}
                      onChange={(e) => setCrisisRegion(e.target.value)}
                      placeholder="Districts, provinces, or river basin"
                      className="rsvp-input"
                    />
                    <p className="text-[11px] text-zinc-400/75 opacity-75 mt-1 font-normal">
                      Example: Colombo & Gampaha Districts
                    </p>
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Center Latitude</label>
                    <input
                      type="number"
                      step="0.0001"
                      value={crisisLat}
                      onChange={(e) => setCrisisLat(Number(e.target.value))}
                      className="rsvp-input font-mono"
                    />
                    <p className="text-[11px] text-zinc-400/75 opacity-75 mt-1 font-normal">
                      Example: 6.9271 (Decimal GPS Latitude)
                    </p>
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Center Longitude</label>
                    <input
                      type="number"
                      step="0.0001"
                      value={crisisLon}
                      onChange={(e) => setCrisisLon(Number(e.target.value))}
                      className="rsvp-input font-mono"
                    />
                    <p className="text-[11px] text-zinc-400/75 opacity-75 mt-1 font-normal">
                      Example: 79.8612 (Decimal GPS Longitude)
                    </p>
                  </div>
                  <div>
                    <label className="block font-bold text-zinc-300 mb-1">Impact Radius (km)</label>
                    <input
                      type="number"
                      min={1}
                      value={crisisRadiusKm}
                      onChange={(e) => setCrisisRadiusKm(Number(e.target.value))}
                      className="rsvp-input font-mono"
                    />
                    <p className="text-[11px] text-zinc-400/75 opacity-75 mt-1 font-normal">
                      Example: 15 km emergency perimeter
                    </p>
                  </div>
                  <div className="md:col-span-3">
                    <label className="block font-bold text-zinc-300 mb-1">Operational Situation Summary</label>
                    <textarea
                      required
                      rows={2}
                      value={crisisDescription}
                      onChange={(e) => setCrisisDescription(e.target.value)}
                      placeholder="Summarize initial field assessment and priority response needs"
                      className="rsvp-input resize-none"
                    />
                    <p className="text-[11px] text-zinc-400/75 opacity-75 mt-1 font-normal">
                      Example: Heavy rainfall caused localized flash flooding; rescue boats, clean water, and emergency medical packs needed.
                    </p>
                  </div>
                  <div className="md:col-span-3 flex justify-end">
                    <motion.button
                      whileTap={{ scale: 0.98 }}
                      type="submit"
                      disabled={commandSubmitting}
                      className="keep-white px-5 py-2.5 rounded-2xl bg-indigo-600 hover:bg-indigo-500 text-white font-extrabold flex items-center gap-2 shadow-sm disabled:opacity-50"
                    >
                      <PlusCircle className="w-4 h-4" />
                      <span>{commandSubmitting ? 'Declaring...' : 'Declare & Activate Crisis Event'}</span>
                    </motion.button>
                  </div>
                </form>
              )}
                </div>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-5">
        <motion.div
          initial={{ opacity: 0, y: 12 }}
          animate={{ opacity: 1, y: 0 }}
          className="lg:col-span-6 bento-glass rounded-3xl p-5 sm:p-6 relative overflow-hidden flex flex-col justify-between"
        >
          <div>
            <div className="flex flex-wrap items-center justify-between gap-3 mb-4 pb-3.5 border-b border-white/[0.08]">
              <div className="flex items-center gap-2.5">
                <div className="w-9 h-9 rounded-2xl bg-indigo-500/20 border border-indigo-500/40 text-indigo-300 flex items-center justify-center shrink-0">
                  <Globe className="w-4 h-4" />
                </div>
                <div>
                  <span className="text-[11px] font-bold uppercase tracking-wider text-indigo-400 block">
                    {t.dashboard.briefingBadge}
                  </span>
                  <h2 className="text-sm sm:text-base font-extrabold text-white">
                    {t.dashboard.briefingHeading} ({briefing?.languageName || activeLangOption.fullName})
                  </h2>
                </div>
              </div>

              <div className="flex flex-wrap items-center gap-1 p-1 rounded-xl bg-black/30 border border-white/[0.08]">
                {LANGUAGE_OPTIONS.map((langOption) => {
                  const isSelected = briefingLang === langOption.code;
                  return (
                    <button
                      key={langOption.code}
                      type="button"
                      onClick={() => onChangeBriefingLanguage(langOption.code)}
                      className={`px-2.5 py-1 rounded-lg text-xs font-bold transition-all ${
                        isSelected
                          ? 'bg-indigo-600 text-white keep-white shadow-sm'
                          : 'text-zinc-400 hover:text-white hover:bg-white/5'
                      }`}
                    >
                      {langOption.label}
                    </button>
                  );
                })}
              </div>
            </div>

            {briefing || crises[0] ? (
              <>
                <h3 className="text-base sm:text-lg font-extrabold text-white tracking-tight leading-snug">
                  {briefing?.headline || crises[0]?.title}
                </h3>
                <p className="text-xs sm:text-sm text-zinc-300 mt-2 leading-relaxed">
                  {briefing?.operationalSummary || crises[0]?.description}
                </p>
              </>
            ) : (
              <div className="py-4 text-xs text-zinc-400">
                <h3 className="text-base font-extrabold text-zinc-200">
                  No Active Crisis Briefing Yet
                </h3>
                <p className="mt-1.5 leading-relaxed">
                  Declare a new Crisis Event in Coordinator Command Actions to generate live multilingual operational briefings.
                </p>
              </div>
            )}
          </div>

          <div className="mt-4 p-3.5 rounded-2xl bg-indigo-500/10 border border-indigo-500/30 flex items-start gap-3">
            <ShieldAlert className="w-4 h-4 text-indigo-400 shrink-0 mt-0.5" />
            <p className="text-xs text-zinc-200 leading-relaxed">
              <strong className="uppercase text-[10px] tracking-wider bg-indigo-600 text-white keep-white px-2 py-0.5 rounded-md mr-2">
                {t.dashboard.safetyAdviceBadge}
              </strong>
              {briefing?.citizenSafetyAdvisory || 'Safety advisories appear automatically once an active crisis and public alerts are published.'}
            </p>
          </div>
        </motion.div>

        {metrics && (
          <div className="lg:col-span-6 grid grid-cols-2 sm:grid-cols-3 gap-3.5">
            <motion.div
              whileHover={{ y: -3, scale: 1.01 }}
              className="bento-glass bento-glass-hover rounded-3xl p-4 flex flex-col justify-between relative overflow-hidden"
            >
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-zinc-400">
                  {t.dashboard.openRequests}
                </span>
                <AlertOctagon className="w-4 h-4 text-rose-400" />
              </div>
              <div className="my-2">
                <span className="text-3xl font-extrabold font-mono text-white">
                  <AnimatedCounter value={metrics.unresolvedCasesCount} />
                </span>
                <span className="text-xs font-mono text-zinc-500 ml-1">
                  / {metrics.totalRequests}
                </span>
              </div>
              <div className="flex items-center gap-1.5 text-xs font-bold text-rose-400">
                <span className="w-2 h-2 rounded-full bg-rose-500 animate-ping" />
                <span>{metrics.criticalOpenCasesCount} {t.dashboard.criticalUrgency}</span>
              </div>
            </motion.div>

            <motion.div
              whileHover={{ y: -3, scale: 1.01 }}
              className="bento-glass bento-glass-hover rounded-3xl p-4 flex flex-col justify-between relative overflow-hidden"
            >
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-zinc-400">
                  {t.dashboard.avgResponseTime}
                </span>
                <Clock className="w-4 h-4 text-indigo-400" />
              </div>
              <div className="my-2">
                <span className="text-3xl font-extrabold font-mono text-indigo-300">
                  <AnimatedCounter value={metrics.averageResponseTimeMinutes} />
                </span>
                <span className="text-xs font-semibold text-zinc-400 ml-1">min</span>
              </div>
              <p className="text-xs text-emerald-400 font-semibold">
                {t.dashboard.underGoal}
              </p>
            </motion.div>

            <motion.div
              whileHover={{ y: -3, scale: 1.01 }}
              className="bento-glass bento-glass-hover rounded-3xl p-4 flex flex-col justify-between relative overflow-hidden"
            >
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-zinc-400">
                  {t.dashboard.suppliesDispatched}
                </span>
                <PackageCheck className="w-4 h-4 text-emerald-400" />
              </div>
              <div className="my-2">
                <span className="text-3xl font-extrabold font-mono text-emerald-300">
                  <AnimatedCounter value={metrics.resourceUtilisationPercentage} />%
                </span>
              </div>
              <div className="w-full h-1.5 bg-white/10 rounded-full overflow-hidden">
                <motion.div
                  initial={{ width: 0 }}
                  animate={{ width: `${Math.min(100, metrics.resourceUtilisationPercentage)}%` }}
                  transition={{ duration: 0.7, ease: 'easeOut' }}
                  className="h-full bg-emerald-400 rounded-full crisync-shimmer-bar"
                />
              </div>
            </motion.div>

            <motion.div
              whileHover={{ y: -3, scale: 1.01 }}
              className="bento-glass bento-glass-hover rounded-3xl p-4 flex flex-col justify-between relative overflow-hidden"
            >
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-zinc-400">
                  {t.dashboard.shelterCapacity}
                </span>
                <Home className="w-4 h-4 text-amber-400" />
              </div>
              <div className="my-2">
                <span className="text-3xl font-extrabold font-mono text-amber-300">
                  <AnimatedCounter value={metrics.shelterOccupancyPercentage} />%
                </span>
              </div>
              <p className="text-xs text-zinc-400">
                {metrics.totalShelterOccupied} / {metrics.totalShelterCapacity} {t.dashboard.spacesFilled}
              </p>
            </motion.div>

            <motion.div
              whileHover={{ y: -3, scale: 1.01 }}
              className="bento-glass bento-glass-hover rounded-3xl p-4 flex flex-col justify-between relative overflow-hidden"
            >
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-zinc-400">
                  {t.dashboard.staffAiReviews}
                </span>
                <Sparkles className="w-4 h-4 text-purple-400" />
              </div>
              <div className="my-2">
                <span className="text-3xl font-extrabold font-mono text-purple-300">
                  <AnimatedCounter value={metrics.aiOverrideFrequencyPercentage} />%
                </span>
              </div>
              <p className="text-xs text-zinc-400">
                {metrics.reviewedAiRecommendations} {t.dashboard.aiScoresReviewed}
              </p>
            </motion.div>

            <motion.div
              whileHover={{ y: -3, scale: 1.01 }}
              className="bento-glass bento-glass-hover rounded-3xl p-4 flex flex-col justify-between relative overflow-hidden"
            >
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-zinc-400">
                  {t.dashboard.duplicatesCaught}
                </span>
                <GitMerge className="w-4 h-4 text-cyan-400" />
              </div>
              <div className="my-2">
                <span className="text-3xl font-extrabold font-mono text-cyan-300">
                  <AnimatedCounter value={metrics.potentialDuplicatesFlaggedCount} />
                </span>
              </div>
              <p className="text-xs text-zinc-400">
                {metrics.duplicateNotificationsPreventedCount} {t.dashboard.duplicateSmsBlocked}
              </p>
            </motion.div>
          </div>
        )}
      </div>

      <div className="bento-glass rounded-3xl p-5 sm:p-6 space-y-5">
        <div className="flex flex-wrap items-center justify-between gap-4 pb-4 border-b border-white/[0.08]">
          <div>
            <div className="flex items-center gap-2">
              <Radio className="w-4 h-4 text-indigo-400" />
              <span className="text-xs font-bold uppercase tracking-wider text-indigo-400">
                {t.dashboard.queueBadge}
              </span>
            </div>
            <h2 className="text-lg sm:text-xl font-extrabold text-white tracking-tight mt-0.5">
              {canManageCases
                ? t.dashboard.queueTitleManage
                : t.dashboard.queueTitleView}
            </h2>
            <p className="text-xs text-zinc-400 mt-0.5">
              {t.dashboard.queueSub}
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-2.5 w-full lg:w-auto">
            <div className="relative flex-1 sm:w-64">
              <Search className="w-4 h-4 text-zinc-500 absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder={t.dashboard.searchPlaceholder}
                className="rsvp-input pl-9 py-2 text-xs"
              />
            </div>

            <div className="mobile-tabs-scroll flex items-center gap-1.5 p-1.5 rounded-2xl bg-black/30 border border-white/[0.08] overflow-x-auto w-full sm:w-auto">
              {[
                { id: 'all', label: `${t.dashboard.filterAll} (${requests.length})` },
                { id: 'critical', label: `${t.dashboard.filterCritical} (${criticalCount})` },
                { id: 'duplicates', label: `${t.dashboard.filterDuplicates} (${duplicateCount})` },
                { id: 'verified', label: t.dashboard.filterVerified }
              ].map((tab) => (
                <button
                  key={tab.id}
                  type="button"
                  onClick={() => setActiveFilter(tab.id as any)}
                  className={`shrink-0 px-3 py-1.5 rounded-xl text-xs font-bold transition ${
                    activeFilter === tab.id
                      ? 'bg-indigo-600 text-white keep-white shadow-sm'
                      : 'text-zinc-400 hover:text-white'
                  }`}
                >
                  {tab.label}
                </button>
              ))}
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
          {filteredRequests.map((req, index) => {
            const isCritical =
              req.finalApprovedUrgency === 'LifeThreatening' || req.finalApprovedUrgency === 'Critical';
            const isExpanded = Boolean(expandedCards[req.id]);
            return (
              <motion.div
                initial={{ opacity: 0, y: 12 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.22, delay: Math.min(index * 0.04, 0.25) }}
                whileHover={{ y: -3 }}
                key={req.id}
                className={`rounded-3xl p-5 border transition-all flex flex-col justify-between relative overflow-hidden ${
                  isCritical
                    ? 'crisync-critical-pulse bg-gradient-to-br from-rose-500/[0.06] via-white/[0.02] to-transparent border-rose-500/35 hover:border-rose-500/50'
                    : 'bg-white/[0.025] border-white/[0.08] hover:border-white/20'
                }`}
              >
                <div>
                  <div className="flex flex-wrap items-start justify-between gap-3 mb-3">
                    <div className="flex items-center gap-3">
                      <div
                        title="AI Priority Score out of 100"
                        className={`w-13 h-13 px-2.5 py-2 rounded-2xl flex flex-col items-center justify-center font-mono border shrink-0 ${
                          isCritical
                            ? 'bg-rose-500/20 border-rose-500/40 text-rose-300'
                            : 'bg-indigo-500/20 border-indigo-500/40 text-indigo-300'
                        }`}
                      >
                        <span className="text-sm font-extrabold leading-none">{req.priorityScore}/100</span>
                        <span className="text-[9px] uppercase tracking-tighter opacity-80 mt-0.5">
                          Priority
                        </span>
                      </div>

                      <div>
                        <div className="flex flex-wrap items-center gap-1.5">
                          <span className="text-xs font-mono font-bold px-2.5 py-0.5 rounded-lg bg-white/10 text-white border border-white/10">
                            {req.referenceCode}
                          </span>
                          <span
                            className={`inline-flex items-center gap-1.5 text-xs font-bold px-2.5 py-0.5 rounded-full border ${
                              isCritical
                                ? 'bg-rose-500/20 text-rose-300 border-rose-500/40'
                                : 'bg-amber-500/20 text-amber-300 border-amber-500/40'
                            }`}
                          >
                            <span
                              className={`w-1.5 h-1.5 rounded-full ${
                                isCritical ? 'bg-rose-400 animate-ping' : 'bg-amber-400'
                              }`}
                            />
                            {t.citizen.urgencies[req.finalApprovedUrgency]?.label || req.finalApprovedUrgency}
                          </span>
                          <span className="text-xs font-bold px-2.5 py-0.5 rounded-full bg-emerald-500/15 border border-emerald-500/30 text-emerald-300">
                            {req.status}
                          </span>
                        </div>

                        <h3 className="text-base font-extrabold text-white mt-1.5 leading-snug">
                          {req.title}
                        </h3>
                      </div>
                    </div>

                    <span className="text-xs font-bold px-3 py-1 rounded-xl bg-white/[0.05] border border-white/10 text-zinc-300">
                      {t.citizen.categories[req.category] || req.category}
                    </span>
                  </div>

                  {req.isFlaggedPotentialDuplicate && (
                    <div className="mb-3 px-3.5 py-2.5 rounded-2xl bg-amber-500/15 border border-amber-500/35 flex flex-wrap items-center justify-between gap-2 text-xs text-amber-200">
                      <span className="inline-flex items-center gap-1.5 font-bold">
                        <GitMerge className="w-4 h-4 text-amber-400" />
                        {t.dashboard.possibleDuplicate} ({Math.round(req.duplicateSimilarityScore * 100)}%)
                      </span>
                    </div>
                  )}

                  <p className="text-xs sm:text-sm text-zinc-300 leading-relaxed mb-3.5">
                    {req.description}
                  </p>

                  <div className="grid grid-cols-1 sm:grid-cols-3 gap-2 p-3 rounded-2xl bg-black/20 border border-white/[0.06] text-xs">
                    <div className="flex items-center gap-1.5 text-zinc-300 truncate">
                      <MapPin className="w-3.5 h-3.5 text-indigo-400 shrink-0" />
                      <span className="truncate">{req.locationAddress}</span>
                    </div>
                    <div className="flex items-center gap-1.5 text-zinc-300">
                      <Users className="w-3.5 h-3.5 text-emerald-400 shrink-0" />
                      <span className="font-bold">{req.affectedPeopleCount}</span>
                      <span className="text-zinc-400">{t.dashboard.peopleAffected}</span>
                    </div>
                    <div className="flex items-center gap-1.5 text-zinc-300">
                      <Package className="w-3.5 h-3.5 text-amber-400 shrink-0" />
                      <span className="font-bold">{req.requestedQuantity}</span>
                      <span className="text-zinc-400">{t.dashboard.unitsNeeded}</span>
                    </div>
                  </div>
                </div>

                <div className="mt-4 pt-3.5 border-t border-white/[0.08] space-y-3">
                  <div className="flex flex-wrap items-center gap-2">
                    {canManageCases && req.status !== 'MergedAsDuplicate' && (
                      <>
                        <motion.button
                          whileHover={{ scale: 1.02 }}
                          whileTap={{ scale: 0.97 }}
                          onClick={() =>
                            onUpdateRequestStatus(
                              req.id,
                              'Verified',
                              'Case verified by staff after reviewing field details.'
                            )
                          }
                          className="keep-white flex-1 py-2 px-3 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white text-xs font-bold flex items-center justify-center gap-1.5 transition shadow-sm"
                        >
                          <CheckCircle2 className="w-3.5 h-3.5" />
                          <span>{t.dashboard.verifyBtn}</span>
                        </motion.button>

                        <motion.button
                          whileHover={{ scale: 1.02 }}
                          whileTap={{ scale: 0.97 }}
                          onClick={() =>
                            onUpdateRequestStatus(
                              req.id,
                              'Escalated',
                              'Case escalated for immediate multi-agency dispatch.'
                            )
                          }
                          className="keep-white flex-1 py-2 px-3 rounded-xl bg-rose-600 hover:bg-rose-500 text-white text-xs font-bold flex items-center justify-center gap-1.5 transition shadow-sm"
                        >
                          <Flame className="w-3.5 h-3.5" />
                          <span>{t.dashboard.escalateBtn}</span>
                        </motion.button>

                        <motion.button
                          whileHover={{ scale: 1.02 }}
                          whileTap={{ scale: 0.97 }}
                          onClick={() =>
                            onUpdateRequestStatus(
                              req.id,
                              'Closed',
                              'Case resolved and closed by case manager.'
                            )
                          }
                          className="py-2 px-3 rounded-xl bg-white/[0.06] hover:bg-white/[0.12] border border-white/10 text-zinc-300 text-xs font-bold flex items-center justify-center gap-1.5 transition"
                        >
                          <Archive className="w-3.5 h-3.5" />
                          <span>{t.dashboard.closeBtn}</span>
                        </motion.button>
                      </>
                    )}

                    {canManageCases && req.isFlaggedPotentialDuplicate && req.status !== 'MergedAsDuplicate' && (
                      <motion.button
                        whileHover={{ scale: 1.02 }}
                        whileTap={{ scale: 0.97 }}
                        onClick={() => onMergeDuplicate(req)}
                        className="keep-white py-2 px-3 rounded-xl bg-amber-600 hover:bg-amber-500 text-white text-xs font-bold flex items-center justify-center gap-1.5 transition shadow-sm"
                      >
                        <GitMerge className="w-3.5 h-3.5" />
                        <span>{t.dashboard.mergeBtn}</span>
                      </motion.button>
                    )}

                    {canReviewAi && (
                      <motion.button
                        whileHover={{ scale: 1.02 }}
                        whileTap={{ scale: 0.97 }}
                        onClick={() => onSelectForAiGovernance(req)}
                        className="py-2 px-3.5 rounded-xl bg-indigo-500/20 hover:bg-indigo-500/30 border border-indigo-500/40 text-indigo-300 text-xs font-bold flex items-center justify-center gap-1.5 transition"
                      >
                        <Sparkles className="w-3.5 h-3.5 text-indigo-400" />
                        <span>{t.dashboard.reviewAiBtn}</span>
                      </motion.button>
                    )}
                  </div>

                  {req.timeline && req.timeline.length > 0 && (
                    <div className="pt-2 border-t border-white/[0.05]">
                      <button
                        type="button"
                        onClick={() => toggleExpandCard(req.id)}
                        className="text-xs font-semibold text-indigo-400 hover:underline flex items-center gap-1"
                      >
                        <History className="w-3.5 h-3.5" />
                        <span>
                          {isExpanded
                            ? 'Hide Activity History'
                            : `Show Activity History (${req.timeline.length} updates)`}
                        </span>
                        {isExpanded ? <ChevronUp className="w-3.5 h-3.5" /> : <ChevronDown className="w-3.5 h-3.5" />}
                      </button>

                      {isExpanded && (
                        <div className="mt-2 space-y-1.5 pl-1">
                          {req.timeline.map((historyItem: any) => (
                            <div
                              key={historyItem.id}
                              className="text-xs text-zinc-400 flex flex-wrap items-center gap-1.5"
                            >
                              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-white/[0.05] text-indigo-300 font-mono text-[11px]">
                                {historyItem.previousStatus}
                                <ArrowRight className="w-2.5 h-2.5" />
                                {historyItem.newStatus}
                              </span>
                              <span>{historyItem.actionDescription}</span>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  )}
                </div>
              </motion.div>
            );
          })}
        </div>
      </div>
    </div>
  );
};
