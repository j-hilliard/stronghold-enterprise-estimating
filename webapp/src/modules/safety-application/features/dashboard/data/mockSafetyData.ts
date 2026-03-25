export type IncidentFilterType = 'recordable' | 'lost-time' | 'near-miss';
export type InvestigationStatusFilter = 'open';

export type SafetyIncident = {
    incidentId: string;
    date: string;
    location: string;
    employee: string;
    incidentType: 'Recordable' | 'Lost Time' | 'Near Miss' | 'First Aid';
    severity: 'High' | 'Medium' | 'Low';
    status: 'Open' | 'Closed' | 'In Review';
    oshaRecordable: boolean;
    lostTime: boolean;
    nearMiss: boolean;
};

export type SafetyInvestigation = {
    investigationId: string;
    relatedIncident: string;
    owner: string;
    openDate: string;
    dueDate: string;
    status: 'Open' | 'Closed' | 'Past Due' | 'In Review';
    priority: 'High' | 'Medium' | 'Low';
};

export const mockIncidents: SafetyIncident[] = [
    { incidentId: 'INC-24017', date: '2026-02-11', location: 'Houston East Yard', employee: 'M. Torres', incidentType: 'Recordable', severity: 'High', status: 'Open', oshaRecordable: true, lostTime: false, nearMiss: false },
    { incidentId: 'INC-24016', date: '2026-02-06', location: 'Dallas Substation', employee: 'J. Cooper', incidentType: 'Lost Time', severity: 'High', status: 'In Review', oshaRecordable: true, lostTime: true, nearMiss: false },
    { incidentId: 'INC-24015', date: '2026-02-03', location: 'Baton Rouge Plant', employee: 'A. Lewis', incidentType: 'Near Miss', severity: 'Medium', status: 'Closed', oshaRecordable: false, lostTime: false, nearMiss: true },
    { incidentId: 'INC-24014', date: '2026-01-29', location: 'San Antonio Shop', employee: 'R. Price', incidentType: 'First Aid', severity: 'Low', status: 'Closed', oshaRecordable: false, lostTime: false, nearMiss: false },
    { incidentId: 'INC-24013', date: '2026-01-24', location: 'Tulsa Field Office', employee: 'K. Nguyen', incidentType: 'Near Miss', severity: 'Low', status: 'Open', oshaRecordable: false, lostTime: false, nearMiss: true },
    { incidentId: 'INC-24012', date: '2026-01-18', location: 'Midland Compressor Site', employee: 'D. Brooks', incidentType: 'Recordable', severity: 'Medium', status: 'In Review', oshaRecordable: true, lostTime: false, nearMiss: false },
    { incidentId: 'INC-24011', date: '2026-01-10', location: 'Corpus Christi Dock', employee: 'S. Patel', incidentType: 'Lost Time', severity: 'High', status: 'Open', oshaRecordable: true, lostTime: true, nearMiss: false },
    { incidentId: 'INC-24010', date: '2026-01-05', location: 'Oklahoma City Terminal', employee: 'B. Evans', incidentType: 'Near Miss', severity: 'Medium', status: 'Closed', oshaRecordable: false, lostTime: false, nearMiss: true },
    { incidentId: 'INC-24009', date: '2025-12-27', location: 'Lubbock Service Center', employee: 'C. Ramirez', incidentType: 'Recordable', severity: 'Medium', status: 'Closed', oshaRecordable: true, lostTime: false, nearMiss: false },
    { incidentId: 'INC-24008', date: '2025-12-21', location: 'Amarillo Line Yard', employee: 'P. Morris', incidentType: 'Near Miss', severity: 'Low', status: 'In Review', oshaRecordable: false, lostTime: false, nearMiss: true },
    { incidentId: 'INC-24007', date: '2025-12-18', location: 'Fort Worth Fabrication', employee: 'E. Ward', incidentType: 'Lost Time', severity: 'High', status: 'Closed', oshaRecordable: true, lostTime: true, nearMiss: false },
    { incidentId: 'INC-24006', date: '2025-12-11', location: 'Shreveport Distribution', employee: 'L. Foster', incidentType: 'Near Miss', severity: 'Low', status: 'Open', oshaRecordable: false, lostTime: false, nearMiss: true },
    { incidentId: 'INC-24005', date: '2025-12-04', location: 'Tyler Equipment Bay', employee: 'N. Hughes', incidentType: 'Recordable', severity: 'High', status: 'Open', oshaRecordable: true, lostTime: false, nearMiss: false },
    { incidentId: 'INC-24004', date: '2025-11-26', location: 'Beaumont Tank Farm', employee: 'H. Cole', incidentType: 'Near Miss', severity: 'Medium', status: 'Closed', oshaRecordable: false, lostTime: false, nearMiss: true },
    { incidentId: 'INC-24003', date: '2025-11-18', location: 'Waco Pipe Yard', employee: 'T. Reed', incidentType: 'Lost Time', severity: 'High', status: 'Open', oshaRecordable: true, lostTime: true, nearMiss: false },
    { incidentId: 'INC-24002', date: '2025-11-10', location: 'Abilene Staging Lot', employee: 'G. Kelly', incidentType: 'Recordable', severity: 'Medium', status: 'In Review', oshaRecordable: true, lostTime: false, nearMiss: false },
    { incidentId: 'INC-24001', date: '2025-11-03', location: 'Lake Charles Terminal', employee: 'V. Jenkins', incidentType: 'Near Miss', severity: 'Low', status: 'Closed', oshaRecordable: false, lostTime: false, nearMiss: true },
];

export const mockInvestigations: SafetyInvestigation[] = [
    { investigationId: 'INV-3108', relatedIncident: 'INC-24017', owner: 'Taylor Webb', openDate: '2026-02-11', dueDate: '2026-02-21', status: 'Open', priority: 'High' },
    { investigationId: 'INV-3107', relatedIncident: 'INC-24016', owner: 'Jordan Bell', openDate: '2026-02-06', dueDate: '2026-02-18', status: 'Past Due', priority: 'High' },
    { investigationId: 'INV-3106', relatedIncident: 'INC-24015', owner: 'Morgan Diaz', openDate: '2026-02-03', dueDate: '2026-02-17', status: 'Closed', priority: 'Medium' },
    { investigationId: 'INV-3105', relatedIncident: 'INC-24013', owner: 'Taylor Webb', openDate: '2026-01-24', dueDate: '2026-02-05', status: 'Open', priority: 'Medium' },
    { investigationId: 'INV-3104', relatedIncident: 'INC-24012', owner: 'Jordan Bell', openDate: '2026-01-18', dueDate: '2026-01-31', status: 'In Review', priority: 'Medium' },
    { investigationId: 'INV-3103', relatedIncident: 'INC-24011', owner: 'Casey Allen', openDate: '2026-01-10', dueDate: '2026-01-24', status: 'Open', priority: 'High' },
    { investigationId: 'INV-3102', relatedIncident: 'INC-24007', owner: 'Morgan Diaz', openDate: '2025-12-18', dueDate: '2026-01-02', status: 'Closed', priority: 'High' },
    { investigationId: 'INV-3101', relatedIncident: 'INC-24005', owner: 'Casey Allen', openDate: '2025-12-04', dueDate: '2025-12-19', status: 'Open', priority: 'High' },
    { investigationId: 'INV-3100', relatedIncident: 'INC-24003', owner: 'Taylor Webb', openDate: '2025-11-18', dueDate: '2025-12-01', status: 'Past Due', priority: 'High' },
];
