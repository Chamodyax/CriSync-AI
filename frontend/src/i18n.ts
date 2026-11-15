export type SupportedLanguage = 'en' | 'si' | 'ta' | 'es' | 'fr' | 'ar';

export interface LanguageOption {
  code: SupportedLanguage;
  label: string;
  fullName: string;
  dir: 'ltr' | 'rtl';
}

export const LANGUAGE_OPTIONS: LanguageOption[] = [
  { code: 'en', label: 'English', fullName: 'English', dir: 'ltr' },
  { code: 'si', label: 'සිංහල', fullName: 'Sinhala (සිංහල)', dir: 'ltr' },
  { code: 'ta', label: 'தமிழ்', fullName: 'Tamil (தமிழ்)', dir: 'ltr' },
  { code: 'es', label: 'Español', fullName: 'Spanish (Español)', dir: 'ltr' },
  { code: 'fr', label: 'Français', fullName: 'French (Français)', dir: 'ltr' },
  { code: 'ar', label: 'العربية', fullName: 'Arabic (العربية)', dir: 'rtl' }
];

export interface QuickTemplateItem {
  label: string;
  category: string;
  urgency: string;
  title: string;
  description: string;
  people: number;
  qty: number;
  wheelchair: boolean;
}

export interface AiSupportFaqItem {
  id: string;
  question: string;
  answer: string;
}

export interface LanguageBundle {
  dir: 'ltr' | 'rtl';
  appSubtitle: string;
  activeCrisisBadge: string;
  switchRole: string;
  signOut: string;
  lightMode: string;
  darkMode: string;
  yourRoleLabel: string;
  currentTabLabel: string;
  languageLabel: string;
  nav: {
    dashboard: string;
    citizen: string;
    aiGovernance: string;
    logistics: string;
    shelters: string;
    volunteers: string;
    donations: string;
    mapResilience: string;
  };
  dashboard: {
    briefingBadge: string;
    briefingHeading: string;
    safetyAdviceBadge: string;
    openRequests: string;
    criticalUrgency: string;
    avgResponseTime: string;
    underGoal: string;
    suppliesDispatched: string;
    shelterCapacity: string;
    spacesFilled: string;
    staffAiReviews: string;
    aiScoresReviewed: string;
    duplicatesCaught: string;
    duplicateSmsBlocked: string;
    queueBadge: string;
    queueTitleManage: string;
    queueTitleView: string;
    queueSub: string;
    searchPlaceholder: string;
    filterAll: string;
    filterCritical: string;
    filterDuplicates: string;
    filterVerified: string;
    verifyBtn: string;
    escalateBtn: string;
    closeBtn: string;
    mergeBtn: string;
    reviewAiBtn: string;
    peopleAffected: string;
    unitsNeeded: string;
    possibleDuplicate: string;
  };
  citizen: {
    portalTitle: string;
    portalSubtitle: string;
    tabAll: string;
    tabRequest: string;
    tabAiSupport: string;
    tabTrack: string;
    tabFamily: string;
    tabShelters: string;
    formBadge: string;
    formTitle: string;
    quickTemplatePrompt: string;
    step1Category: string;
    step2Urgency: string;
    step3Summary: string;
    step3Placeholder: string;
    step4Description: string;
    step4Placeholder: string;
    step5Location: string;
    locationPlaceholder: string;
    peopleCountLabel: string;
    quantityLabel: string;
    photoLabel: string;
    wheelchairCheckbox: string;
    submitRequestBtn: string;
    familyBadge: string;
    familyTitle: string;
    headNameLabel: string;
    totalMembersLabel: string;
    childrenLabel: string;
    elderlyLabel: string;
    medicalNotesLabel: string;
    mobilityCheckbox: string;
    chronicCheckbox: string;
    saveFamilyBtn: string;
    alertsTitle: string;
    safetyWhatToDo: string;
    alertHeadline: string;
    alertBody: string;
    alertGuidance: string;
    sheltersTitle: string;
    spacesOpen: string;
    trackerBadge: string;
    trackerTitle: string;
    rateDeliveryBtn: string;
    feedbackTitle: string;
    ratingLabel: string;
    commentsLabel: string;
    submitFeedbackBtn: string;
    aiAssistantBadge: string;
    aiAssistantTitle: string;
    aiAssistantSubtitle: string;
    aiAskPlaceholder: string;
    aiAskBtn: string;
    aiOfficialBriefingTitle: string;
    categories: Record<string, string>;
    urgencies: Record<string, { label: string; desc: string }>;
    templates: QuickTemplateItem[];
    aiFaqs: AiSupportFaqItem[];
  };
  smsNotification: {
    title: string;
    subtitle: string;
    languageSelectLabel: string;
    subject: string;
    body: string;
    sendBtn: string;
  };
}

export const TRANSLATIONS: Record<SupportedLanguage, LanguageBundle> = {
  en: {
    dir: 'ltr',
    appSubtitle: 'Humanitarian Aid & Emergency Coordination System',
    activeCrisisBadge: 'Kalu Ganga Floods Response',
    switchRole: 'Switch Role',
    signOut: 'Sign Out',
    lightMode: 'Light',
    darkMode: 'Dark',
    yourRoleLabel: 'Your Role',
    currentTabLabel: 'Current Tab',
    languageLabel: 'Language',
    nav: {
      dashboard: 'Overview & Cases',
      citizen: 'Request Help & My Aid',
      aiGovernance: 'AI Priority & Forecast',
      logistics: 'Supplies & Dispatch',
      shelters: 'Evacuation Shelters',
      volunteers: 'Volunteers & Teams',
      donations: 'Relief Campaigns',
      mapResilience: 'Live Map & System Health'
    },
    dashboard: {
      briefingBadge: 'Official AI Multilingual Situation Summary',
      briefingHeading: 'Crisis Briefing',
      safetyAdviceBadge: 'Safety Advice',
      openRequests: 'Open Requests',
      criticalUrgency: 'Critical Urgency',
      avgResponseTime: 'Avg. Response Time',
      underGoal: '✓ Under 30-min goal',
      suppliesDispatched: 'Supplies Dispatched',
      shelterCapacity: 'Shelter Capacity',
      spacesFilled: 'spaces filled',
      staffAiReviews: 'Staff AI Reviews',
      aiScoresReviewed: 'AI scores reviewed',
      duplicatesCaught: 'Duplicates Caught',
      duplicateSmsBlocked: 'duplicate SMS blocked',
      queueBadge: 'Citizen Help Requests Queue',
      queueTitleManage: 'Review, Verify & Prioritize Incoming Help Requests',
      queueTitleView: 'Active Community Assistance Requests',
      queueSub: 'Each card shows the AI Urgency Score (out of 100) and quick buttons to verify, escalate, or merge duplicate reports.',
      searchPlaceholder: 'Search by code, town, or need...',
      filterAll: 'All',
      filterCritical: 'Critical',
      filterDuplicates: 'Duplicates',
      filterVerified: 'Verified',
      verifyBtn: 'Verify Request',
      escalateBtn: 'Escalate Urgent',
      closeBtn: 'Close Case',
      mergeBtn: 'Merge Duplicate',
      reviewAiBtn: 'Review AI Score',
      peopleAffected: 'people affected',
      unitsNeeded: 'units needed',
      possibleDuplicate: 'Possible Duplicate Report'
    },
    citizen: {
      portalTitle: 'Citizen Emergency Help & Multilingual AI Support Portal',
      portalSubtitle: 'Submit assistance requests in your preferred language, receive automated AI safety guidance, and track relief deliveries.',
      tabAll: 'Show All Tools',
      tabRequest: '1. Request Help',
      tabAiSupport: '2. AI Multilingual Support',
      tabTrack: '3. My Requests',
      tabFamily: '4. Family Profile',
      tabShelters: '5. Shelters & Alerts',
      formBadge: 'Multilingual Emergency Request',
      formTitle: 'Ask for Immediate Relief Assistance',
      quickTemplatePrompt: 'In a hurry? Click a Quick Emergency Template to auto-fill in English:',
      step1Category: '1. What kind of help do you need?',
      step2Urgency: '2. How urgent is your situation?',
      step3Summary: '3. Short Summary of What You Need *',
      step3Placeholder: 'Example: Need insulin and dry food packs for family of 6',
      step4Description: '4. Explain Your Situation (Water level, medical needs, landmarks) *',
      step4Placeholder: 'Tell responders about any medical conditions, children/elderly, or blocked roads...',
      step5Location: '5. Your Location (Click a town or type your address)',
      locationPlaceholder: 'Your street address, village, or nearest landmark',
      peopleCountLabel: 'People Affected',
      quantityLabel: 'Quantity / Packs Needed',
      photoLabel: 'Photo / Medical Note (Optional)',
      wheelchairCheckbox: 'Someone in my family needs wheelchair or disability-friendly evacuation support',
      submitRequestBtn: 'Submit Emergency Help Request',
      familyBadge: 'Saved Family Details',
      familyTitle: 'My Family & Medical Profile',
      headNameLabel: 'Head of Family Name',
      totalMembersLabel: 'Total Members',
      childrenLabel: 'Children',
      elderlyLabel: 'Elderly (65+)',
      medicalNotesLabel: 'Important Medical or Dietary Needs',
      mobilityCheckbox: 'Wheelchair / Mobility Assistance Required',
      chronicCheckbox: 'Ongoing Medical Condition (Insulin, Oxygen, Dialysis)',
      saveFamilyBtn: 'Save Family Profile',
      alertsTitle: 'Official Flood Warnings & Safety Advice',
      safetyWhatToDo: 'What to do:',
      alertHeadline: 'Kalu Ganga Basin High Flood Warning — Ratnapura & Kalutara',
      alertBody: 'Heavy monsoon rainfall has raised water levels in Ratnapura, Kuruwita, and Elapatha. Low-lying roads are submerged.',
      alertGuidance: 'Move immediately to approved high-ground shelters. Bring essential medicines, ID documents, and drinking water.',
      sheltersTitle: 'Open Evacuation Shelters Near You',
      spacesOpen: 'spaces open',
      trackerBadge: 'Live Aid Status Tracker',
      trackerTitle: 'My Submitted Help Requests & Delivery Rating',
      rateDeliveryBtn: 'Rate Delivery',
      feedbackTitle: 'Confirm Aid Received & Rate Your Experience',
      ratingLabel: 'Your Rating (1 to 5 Stars)',
      commentsLabel: 'Comments for the Relief Team',
      submitFeedbackBtn: 'Submit Feedback',
      aiAssistantBadge: 'Coursework 1.1.A • Automated Multilingual AI Support',
      aiAssistantTitle: 'AI Humanitarian Support & Multilingual Communication Center',
      aiAssistantSubtitle: 'Receive instant safety instructions, shelter directions, and emergency guidance in your chosen language.',
      aiAskPlaceholder: 'Ask a question (e.g., Where is a wheelchair shelter? How do I get insulin?)...',
      aiAskBtn: 'Get AI Help',
      aiOfficialBriefingTitle: 'Live Multilingual Crisis Broadcast',
      categories: {
        Medicine: 'Medicine & First Aid',
        Food: 'Food & Dry Rations',
        WaterAndSanitation: 'Clean Drinking Water',
        TemporaryShelter: 'Safe Shelter',
        SearchAndRescue: 'Boat Rescue / Evacuation',
        AccessibilityAssistance: 'Wheelchair / Disability Aid',
        Transportation: 'Emergency Transport'
      },
      urgencies: {
        Low: { label: 'Low', desc: 'Can wait 24+ hrs' },
        Medium: { label: 'Medium', desc: 'Needed today' },
        High: { label: 'High', desc: 'Urgent need' },
        Critical: { label: 'Critical', desc: 'Very urgent' },
        LifeThreatening: { label: 'Life Threatening', desc: 'Immediate danger' }
      },
      templates: [
        {
          label: '💊 Urgent Medicine / Insulin',
          category: 'Medicine',
          urgency: 'LifeThreatening',
          title: 'Urgent cold-chain insulin & medical supplies needed',
          description: 'Flood waters have cut off road access. Elderly diabetic family member needs insulin and daily medication urgently.',
          people: 6,
          qty: 10,
          wheelchair: true
        },
        {
          label: '🍲 Family Food & Clean Water',
          category: 'Food',
          urgency: 'High',
          title: 'Dry rations and drinking water for marooned family',
          description: 'Ground floor is flooded and kitchen supplies are damaged. Need dry food packs and clean drinking water for children and adults.',
          people: 5,
          qty: 15,
          wheelchair: false
        },
        {
          label: '🚣 Boat Rescue / Evacuation',
          category: 'SearchAndRescue',
          urgency: 'LifeThreatening',
          title: 'Immediate boat evacuation needed due to rising water',
          description: 'Water level is rising rapidly near our home. Family is waiting on the upper floor and needs safe transport to high ground.',
          people: 4,
          qty: 1,
          wheelchair: true
        },
        {
          label: '♿ Wheelchair Safe Shelter',
          category: 'TemporaryShelter',
          urgency: 'Critical',
          title: 'Need wheelchair-accessible shelter space for family',
          description: 'Evacuating from flood zone with an elderly relative who uses a wheelchair. Need a ground-floor accessible relief shelter.',
          people: 6,
          qty: 6,
          wheelchair: true
        }
      ],
      aiFaqs: [
        {
          id: 'shelter',
          question: 'Where is the nearest wheelchair-accessible shelter?',
          answer: 'Ratnapura Central High-Ground Relief Complex (Main Street, Ratnapura) and Kalutara District Maha Vidyalaya Shelter are open right now with wheelchair ramps, medical bays, and clean water.'
        },
        {
          id: 'medicine',
          question: 'How do I get urgent insulin or refrigerated medicine?',
          answer: 'Select "Medicine & First Aid" and set urgency to "Life Threatening". Our AI Triage Engine flags cold-chain medical keywords (like insulin or oxygen) for immediate priority dispatch from the Ratnapura Medical Cold Depot.'
        },
        {
          id: 'rescue',
          question: 'Water is rising around our house. What should we do?',
          answer: 'Move all family members to the highest floor or roof immediately, turn off main electricity, and submit a "Boat Rescue / Evacuation" request with your town preset so field rescue teams receive your GPS pin.'
        },
        {
          id: 'triage',
          question: 'Who reviews my emergency request?',
          answer: 'Our AI calculates an initial Priority Score (0–100) based on urgency and family vulnerability, and an authorised human Case Manager or Emergency Coordinator verifies and dispatches aid.'
        }
      ]
    },
    smsNotification: {
      title: 'Multilingual SMS Alert & Duplicate Protection Test',
      subtitle: 'Select a language to generate an emergency SMS in that language. Sending twice with the same Message ID blocks duplicate texts.',
      languageSelectLabel: 'Notification Language',
      subject: 'Emergency Evacuation Update',
      body: 'Proceed immediately to Ratnapura Central High-Ground Relief Complex. Medical & food aid available.',
      sendBtn: 'Send Multilingual Test SMS'
    }
  },

  si: {
    dir: 'ltr',
    appSubtitle: 'මානුෂීය ආධාර සහ හදිසි ආපදා සම්බන්ධීකරණ පද්ධතිය',
    activeCrisisBadge: 'කළු ගඟ ගංවතුර ආපදා මෙහෙයුම',
    switchRole: 'භූමිකාව මාරු කරන්න',
    signOut: 'ඉවත් වන්න',
    lightMode: 'දිවා ආලෝකය',
    darkMode: 'අඳුරු තේමාව',
    yourRoleLabel: 'ඔබගේ භූමිකාව',
    currentTabLabel: 'වත්මන් පිටුව',
    languageLabel: 'භාෂාව',
    nav: {
      dashboard: 'ප්‍රධාන පුවරුව සහ ඉල්ලීම්',
      citizen: 'ආධාර ඉල්ලීම් සහ මගේ තොරතුරු',
      aiGovernance: 'AI ප්‍රමුඛතා සහ පුරෝකථන',
      logistics: 'සැපයුම් සහ බෙදාහැරීම්',
      shelters: 'ආරක්ෂිත මධ්‍යස්ථාන',
      volunteers: 'ස්වේච්ඡා කණ්ඩායම්',
      donations: 'ආධාර අරමුදල්',
      mapResilience: 'සජීවී සිතියම සහ පද්ධති තත්ත්වය'
    },
    dashboard: {
      briefingBadge: 'නිල බහුභාෂා ආපදා තත්ත්ව වාර්තාව',
      briefingHeading: 'ආපදා මෙහෙයුම් සාරාංශය',
      safetyAdviceBadge: 'ආරක්ෂක උපදෙස්',
      openRequests: 'විවෘත ඉල්ලීම්',
      criticalUrgency: 'අතිශය හදිසි',
      avgResponseTime: 'සාමාන්‍ය ප්‍රතිචාර කාලය',
      underGoal: '✓ මිනිත්තු 30 ඉලක්කය තුළ',
      suppliesDispatched: 'බෙදාහැරි සැපයුම්',
      shelterCapacity: 'කඳවුරු ධාරිතාව',
      spacesFilled: 'ඉඩ ප්‍රමාණයක් පිරී ඇත',
      staffAiReviews: 'නිලධාරී AI පරීක්ෂාවන්',
      aiScoresReviewed: 'AI ලකුණු පරීක්ෂා කරන ලදී',
      duplicatesCaught: 'හඳුනාගත් අනුපිටපත්',
      duplicateSmsBlocked: 'අනුපිටපත් SMS වැළැක්විය',
      queueBadge: 'ජනතා සහන ඉල්ලීම් පෙළගැස්ම',
      queueTitleManage: 'ලැබී ඇති හදිසි ආධාර ඉල්ලීම් පරීක්ෂා කිරීම සහ ප්‍රමුඛතා ලබාදීම',
      queueTitleView: 'ක්‍රියාත්මක ප්‍රජා සහන ඉල්ලීම්',
      queueSub: 'සෑම ඉල්ලීමකම AI ප්‍රමුඛතා ලකුණු (100 න්) සහ තහවුරු කිරීමේ බොත්තම් දැක්වේ.',
      searchPlaceholder: 'අංකය, නගරය හෝ අවශ්‍යතාවය අනුව සොයන්න...',
      filterAll: 'සියල්ල',
      filterCritical: 'අතිශය හදිසි',
      filterDuplicates: 'අනුපිටපත්',
      filterVerified: 'තහවුරු කළ',
      verifyBtn: 'තහවුරු කරන්න',
      escalateBtn: 'හදිසි ලෙස යොමු කරන්න',
      closeBtn: 'අවසන් කරන්න',
      mergeBtn: 'අනුපිටපත් එක් කරන්න',
      reviewAiBtn: 'AI ලකුණු බලන්න',
      peopleAffected: 'පුද්ගලයින්',
      unitsNeeded: 'ඒකක අවශ්‍යයි',
      possibleDuplicate: 'හඳුනාගත් අනුපිටපත් ඉල්ලීමක්'
    },
    citizen: {
      portalTitle: 'පුරවැසි හදිසි සහන සහ බහුභාෂා AI උපකාරක ද්වාරය',
      portalSubtitle: 'සිංහල භාෂාවෙන් හදිසි ආධාර ඉල්ලුම් කරන්න, ස්වයංක්‍රීය AI ආරක්ෂක උපදෙස් ලබාගන්න, සහ ආධාර ලැබෙන ආකාරය පරීක්ෂා කරන්න.',
      tabAll: 'සියලුම සේවාවන්',
      tabRequest: '1. ආධාර ඉල්ලුම් කරන්න',
      tabAiSupport: '2. AI සිංහල උපකාරක සේවාව',
      tabTrack: '3. මගේ ඉල්ලීම්',
      tabFamily: '4. පවුලේ තොරතුරු',
      tabShelters: '5. ආරක්ෂිත කඳවුරු සහ නිවේදන',
      formBadge: 'හදිසි සහන ඉල්ලුම්පත (සිංහල)',
      formTitle: 'හදිසි මානුෂීය ආධාර ඉල්ලුම් කරන්න',
      quickTemplatePrompt: 'ඉක්මනින් ඉල්ලුම් කිරීමට පහත ක්ෂණික ආකෘතියක් (Template) තෝරන්න:',
      step1Category: '1. ඔබට අවශ්‍ය කුමන ආකාරයේ උපකාරයක්ද?',
      step2Urgency: '2. තත්ත්වය කොපමණ හදිසිද?',
      step3Summary: '3. අවශ්‍යතාවය පිළිබඳ කෙටි සාරාංශයක් *',
      step3Placeholder: 'උදා: සාමාජිකයින් 6 දෙනෙකුට ඉන්සියුලින් සහ වියළි ආහාර අවශ්‍යයි',
      step4Description: '4. වැඩිදුර විස්තර (ජල මට්ටම, රෝගීන්, ගමන් මාර්ග අවහිරතා) *',
      step4Placeholder: 'වෛද්‍ය අවශ්‍යතා, කුඩා දරුවන්/වැඩිහිටියන් හෝ අවහිර වූ මාර්ග ගැන සඳහන් කරන්න...',
      step5Location: '5. ඔබ සිටින ස්ථානය (නගරය තෝරන්න හෝ ලිපිනය ලියන්න)',
      locationPlaceholder: 'ඔබගේ ලිපිනය, ගම හෝ ආසන්නතම ස්ථානය',
      peopleCountLabel: 'බලපෑමට ලක්වූ පිරිස',
      quantityLabel: 'අවශ්‍ය ප්‍රමාණය (ඒකක)',
      photoLabel: 'ඡායාරූප / වෛද්‍ය වාර්තා (අත්‍යවශ්‍ය නොවේ)',
      wheelchairCheckbox: 'රෝද පුටු හෝ විශේෂ අවශ්‍යතා සහිත පුද්ගලයෙකු පවුලේ සිටී',
      submitRequestBtn: 'හදිසි ආධාර ඉල්ලීම යොමු කරන්න',
      familyBadge: 'සුරැකි පවුලේ තොරතුරු',
      familyTitle: 'මගේ පවුලේ සහ වෛද්‍ය තොරතුරු',
      headNameLabel: 'ගෘහ මූලිකයාගේ නම',
      totalMembersLabel: 'මුළු සාමාජිකයින්',
      childrenLabel: 'දරුවන් ගණන',
      elderlyLabel: 'වැඩිහිටියන් (65+)',
      medicalNotesLabel: 'විශේෂ වෛද්‍ය හෝ ආහාර අවශ්‍යතා',
      mobilityCheckbox: 'රෝද පුටු / ගමන් බිමන් උපකාර අවශ්‍යයි',
      chronicCheckbox: 'නිදන්ගත රෝග පවතී (ඉන්සියුලින්, ඔක්සිජන්)',
      saveFamilyBtn: 'පවුලේ තොරතුරු සුරකින්න',
      alertsTitle: 'නිල ගංවතුර අනතුරු ඇඟවීම් සහ ආරක්ෂක උපදෙස්',
      safetyWhatToDo: 'කළ යුතු දේ:',
      alertHeadline: 'කළු ගඟ ද්‍රෝණියේ දැඩි ගංවතුර අනතුරු ඇඟවීම — රත්නපුර සහ කළුතර',
      alertBody: 'ධාරානිපාත වර්ෂාව හේතුවෙන් රත්නපුර, කුරුවිට සහ ඇලපාත පහත් බිම් ජලයෙන් යටවී ඇත.',
      alertGuidance: 'වහාම අනුමත ආරක්ෂිත මධ්‍යස්ථාන වෙත යොමු වන්න. අත්‍යවශ්‍ය ඖෂධ සහ පානීය ජලය රැගෙන යන්න.',
      sheltersTitle: 'ඔබ අසල විවෘත ආරක්ෂිත කඳවුරු',
      spacesOpen: 'ඉඩ පහසුකම් ඇත',
      trackerBadge: 'සජීවී ආධාර තත්ත්ව පරීක්ෂාව',
      trackerTitle: 'මා යොමු කළ ඉල්ලීම් සහ ආධාර ඇගයීම',
      rateDeliveryBtn: 'ආධාර ඇගයීම',
      feedbackTitle: 'ආධාර ලැබුණු බව තහවුරු කර තරු ලකුණු ලබාදෙන්න',
      ratingLabel: 'ඔබගේ ඇගයීම (තරු 1 සිට 5 දක්වා)',
      commentsLabel: 'සහන කණ්ඩායම වෙත ඔබගේ අදහස්',
      submitFeedbackBtn: 'ඇගයීම යොමු කරන්න',
      aiAssistantBadge: 'Coursework 1.1.A • ස්වයංක්‍රීය බහුභාෂා AI සහායක',
      aiAssistantTitle: 'AI මානුෂීය උපකාරක සහ සිංහල සන්නිවේදන මධ්‍යස්ථානය',
      aiAssistantSubtitle: 'ඔබගේ භාෂාවෙන් ක්ෂණික ආරක්ෂක උපදෙස්, ආරක්ෂිත කඳවුරු තොරතුරු සහ හදිසි මගපෙන්වීම් ලබාගන්න.',
      aiAskPlaceholder: 'ප්‍රශ්නයක් අසන්න (උදා: රෝද පුටු පහසුකම් ඇති කඳවුර කොහේද? ඉන්සියුලින් ලබාගන්නේ කෙසේද?)...',
      aiAskBtn: 'AI පිළිතුර ලබාගන්න',
      aiOfficialBriefingTitle: 'සජීවී බහුභාෂා ආපදා නිවේදනය',
      categories: {
        Medicine: 'ඖෂධ සහ ප්‍රථමාධාර',
        Food: 'ආහාර සහ වියළි සලාක',
        WaterAndSanitation: 'පිරිසිදු පානීය ජලය',
        TemporaryShelter: 'ආරක්ෂිත නවාතැන්',
        SearchAndRescue: 'බෝට්ටු මගින් මුදාගැනීම්',
        AccessibilityAssistance: 'රෝද පුටු / ආබාධිත සහන',
        Transportation: 'හදිසි ප්‍රවාහන පහසුකම්'
      },
      urgencies: {
        Low: { label: 'අඩු (Low)', desc: 'පැය 24+ රැඳිය හැක' },
        Medium: { label: 'මධ්‍යම (Medium)', desc: 'අද දින අවශ්‍යයි' },
