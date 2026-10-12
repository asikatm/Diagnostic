/* Setup screens. Each entry describes the grid columns and form fields of one table; MasterPage renders it. */
'use strict';

const GENDER = ['Male', 'Female', 'Other'];
const yes = v => v ? '<i class="bi bi-check-circle-fill text-success"></i>' : '<i class="bi bi-dash text-muted"></i>';

// Common fields
const F = {
    code: { key: 'code', label: 'Code', col: 4 },
    name: { key: 'name', label: 'Name', required: true, col: 8 },
    sort: { key: 'sortOrder', label: 'Sort Order', type: 'number', col: 4, default: 0 },
    active: { key: 'isActive', label: 'Active', type: 'check', col: 4, default: true },
};
const C = {
    code: { key: 'code', label: 'Code' },
    name: { key: 'name', label: 'Name' },
    sort: { key: 'sortOrder', label: 'Sort' },
    active: { key: 'isActive', label: 'Active', type: 'bool' },
};

const MASTERS = {
    // =========================== Administrative ===========================
    Tenant: {
        title: 'Tenants', api: '/api/admin/tenants', sub: 'Branches / diagnostic centres',
        columns: [C.code, C.name, { key: 'shortName', label: 'Short' }, { key: 'phone', label: 'Phone' }, { key: 'address', label: 'Address' }, C.active],
        fields: [F.code, F.name, { key: 'shortName', label: 'Short Name', col: 4 }, { key: 'phone', label: 'Phone', col: 4 }, { key: 'email', label: 'Email', col: 4 },
            { key: 'address', label: 'Address', type: 'textarea', col: 12 }, { key: 'logoPath', label: 'Logo file path (on report server)', col: 8 }, F.active],
    },
    Department: {
        title: 'Departments', api: '/api/admin/departments',
        columns: [C.code, C.name, { key: 'tenant.name', label: 'Tenant' }, C.sort, C.active],
        fields: [F.code, F.name, { key: 'tenantId', label: 'Tenant', type: 'select', lookup: '/api/admin/tenants/lookup', col: 8 }, F.sort, F.active],
    },
    ServiceUnit: {
        title: 'Service Units', api: '/api/admin/service-units',
        columns: [C.code, C.name, { key: 'department.name', label: 'Department' }, C.sort, C.active],
        fields: [F.code, F.name, { key: 'departmentId', label: 'Department', type: 'select', lookup: '/api/admin/departments/lookup', col: 8 }, F.sort, F.active],
    },
    Role: {
        title: 'Roles', api: '/api/admin/roles', sub: 'Assign menu rights from Menu Permission',
        columns: [C.code, C.name, { key: 'description', label: 'Description' }, { key: 'isSuperAdmin', label: 'Super Admin', type: 'bool' }, C.active],
        fields: [F.code, F.name, { key: 'description', label: 'Description', col: 12 },
            { key: 'isSuperAdmin', label: 'Super admin (full access)', type: 'check', col: 6 }, F.sort, F.active],
    },
    User: {
        title: 'Users', api: '/api/admin/users',
        columns: [{ key: 'userName', label: 'User Name' }, { key: 'fullName', label: 'Full Name' }, { key: 'roles', label: 'Roles' },
            { key: 'tenantName', label: 'Tenant' }, { key: 'phone', label: 'Phone' }, { key: 'lastLoginAt', label: 'Last Login', type: 'datetime' }, C.active],
        fields: [
            { key: 'userName', label: 'User Name', required: true, col: 6 }, { key: 'fullName', label: 'Full Name', required: true, col: 6 },
            { key: 'password', label: 'Password', type: 'password', col: 6, help: 'Min 6 characters. Leave blank to keep the current password.' },
            { key: 'designation', label: 'Designation', col: 6 },
            { key: 'email', label: 'Email', col: 6 }, { key: 'phone', label: 'Phone', col: 6 },
            { key: 'tenantId', label: 'Tenant', type: 'select', lookup: '/api/admin/tenants/lookup', col: 6 },
            { key: 'departmentId', label: 'Department', type: 'select', lookup: '/api/admin/departments/lookup', col: 6 },
            { key: 'roleIds', label: 'Roles', type: 'multiselect', lookup: '/api/admin/roles/lookup', col: 12 },
            F.active],
    },
    Project: {
        title: 'Projects', api: '/api/admin/projects', sub: 'Modules shown as tiles on the home screen',
        columns: [C.code, C.name, { key: 'icon', label: 'Icon', render: r => `<i class="bi ${App.esc(r.icon)}"></i> ${App.esc(r.icon)}` }, C.sort, C.active],
        fields: [F.code, F.name, { key: 'icon', label: 'Icon (Bootstrap icon class)', col: 6, help: 'e.g. bi-clipboard2-check' }, { key: 'url', label: 'Url', col: 6 }, F.sort, F.active],
    },
    Menu: {
        title: 'Project Menus', api: '/api/admin/menus', sub: 'The menu code links a screen to its API permission',
        filter: { key: 'projectId', label: 'Project', lookup: '/api/admin/projects/lookup' },
        columns: [{ key: 'project.name', label: 'Project' }, C.code, C.name, { key: 'url', label: 'Url' },
            { key: 'icon', label: 'Icon', render: r => `<i class="bi ${App.esc(r.icon)}"></i>` }, C.sort, C.active],
        fields: [{ key: 'projectId', label: 'Project', type: 'select', lookup: '/api/admin/projects/lookup', required: true, col: 6 },
            { key: 'parentId', label: 'Parent Menu', type: 'select', lookup: '/api/admin/menus/lookup', col: 6 },
            F.code, F.name, { key: 'url', label: 'Url', col: 6 }, { key: 'icon', label: 'Icon', col: 6 }, F.sort, F.active],
    },
    PaymentMode: {
        title: 'Payment Modes', api: '/api/admin/payment-modes',
        columns: [C.code, C.name, { key: 'requiresReference', label: 'Needs Ref.', type: 'bool' }, C.sort, C.active],
        fields: [F.code, F.name, { key: 'requiresReference', label: 'Transaction reference required', type: 'check', col: 8 }, F.sort, F.active],
    },
    PaymentChannel: {
        title: 'Payment Channels', api: '/api/admin/payment-channels', sub: 'Card types, mobile banking wallets, bank accounts',
        columns: [C.code, C.name, { key: 'paymentMode.name', label: 'Mode' }, { key: 'accountNo', label: 'Account No' }, C.sort, C.active],
        fields: [F.code, F.name, { key: 'paymentModeId', label: 'Payment Mode', type: 'select', lookup: '/api/admin/payment-modes/lookup', required: true, col: 6 },
            { key: 'accountNo', label: 'Account / Merchant No', col: 6 }, F.sort, F.active],
    },
    Country: {
        title: 'Country / Nationality', api: '/api/admin/countries',
        columns: [C.code, C.name, { key: 'nationality', label: 'Nationality' }, { key: 'phoneCode', label: 'Phone Code' }, C.active],
        fields: [F.code, F.name, { key: 'nationality', label: 'Nationality', col: 6 }, { key: 'phoneCode', label: 'Phone Code', col: 6 }, F.sort, F.active],
    },
    Religion: {
        title: 'Religions', api: '/api/admin/religions',
        columns: [C.code, C.name, C.sort, C.active], fields: [F.code, F.name, F.sort, F.active],
    },
    Printer: {
        title: 'Printers', api: '/api/admin/printers',
        columns: [C.code, C.name, { key: 'printerType', label: 'Type' }, { key: 'paperSize', label: 'Paper' }, { key: 'printerPath', label: 'Path' },
            { key: 'tenant.name', label: 'Tenant' }, { key: 'isDefault', label: 'Default', type: 'bool' }, C.active],
        fields: [F.code, F.name, { key: 'printerType', label: 'Type', type: 'enum', options: ['A4', 'Sticker', 'Pos'], col: 4, default: 'A4' },
            { key: 'paperSize', label: 'Paper Size', col: 4 }, { key: 'printerPath', label: 'Printer path / share', col: 4 },
            { key: 'tenantId', label: 'Tenant', type: 'select', lookup: '/api/admin/tenants/lookup', col: 6 },
            { key: 'isDefault', label: 'Default', type: 'check', col: 3 }, F.active],
    },
    AppGlobalSetting: {
        title: 'App Global Settings', api: '/api/admin/global-settings',
        columns: [{ key: 'settingGroup', label: 'Group' }, { key: 'settingKey', label: 'Key' }, { key: 'settingValue', label: 'Value' }, { key: 'description', label: 'Description' }, C.active],
        fields: [{ key: 'settingGroup', label: 'Group', col: 4 }, { key: 'settingKey', label: 'Key', required: true, col: 8 },
            { key: 'settingValue', label: 'Value', type: 'textarea', col: 12 }, { key: 'description', label: 'Description', col: 12 }, F.active],
    },
    Report: {
        title: 'Reports and SP Mapping', api: '/api/admin/reports', size: 'modal-xl',
        sub: 'Each report runs a stored procedure on the report server',
        columns: [C.code, C.name, { key: 'procedureName', label: 'Stored Procedure' }, { key: 'template', label: 'Template' },
            { key: 'parameters', label: 'Params', render: r => (r.parameters || []).map(p => App.esc(p.paramName)).join(', ') }, C.active],
        fields: [F.code, F.name, { key: 'procedureName', label: 'Stored Procedure', required: true, col: 6, help: 'schema.name, e.g. diag.usp_Rpt_DueList' },
            { key: 'template', label: 'Template', type: 'enum', options: ['Tabular', 'LabReport', 'MoneyReceipt', 'Sticker'], col: 3, default: 'Tabular' },
            { key: 'orientation', label: 'Orientation', type: 'enum', options: ['Portrait', 'Landscape'], col: 3, default: 'Portrait' },
            { key: 'projectId', label: 'Project', type: 'select', lookup: '/api/admin/projects/lookup', col: 6 },
            { key: 'description', label: 'Description', col: 6 }, F.sort, F.active],
        children: {
            key: 'parameters', label: 'Stored procedure parameters',
            fields: [{ key: 'paramName', label: 'Parameter (@Name)', required: true }, { key: 'label', label: 'Label', required: true },
                { key: 'dataType', label: 'Type', type: 'enum', options: ['date', 'int', 'decimal', 'string', 'bool', 'testgroup', 'media'], default: 'date' },
                { key: 'isRequired', label: 'Req.', type: 'check' }, { key: 'defaultValue', label: 'Default' },
                { key: 'sortOrder', label: 'Sort', type: 'number', default: 0 }],
        },
    },

    // =========================== Diagnostic ===========================
    ParentGroup: {
        title: 'Parent Groups', api: '/api/diag/parent-groups', sub: 'Pathology, Radiology & Imaging, Cardiology …',
        columns: [C.code, C.name, C.sort, C.active], fields: [F.code, F.name, F.sort, F.active],
    },
    TestGroup: {
        title: 'Test Groups', api: '/api/diag/test-groups',
        filter: { key: 'parentGroupId', label: 'Parent Group', lookup: '/api/diag/parent-groups/lookup' },
        columns: [C.code, C.name, { key: 'parentGroup.name', label: 'Parent Group' }, { key: 'department.name', label: 'Department' },
            { key: 'reportHeading', label: 'Report Heading' }, C.sort, C.active],
        fields: [F.code, F.name, { key: 'parentGroupId', label: 'Parent Group', type: 'select', lookup: '/api/diag/parent-groups/lookup', required: true, col: 6 },
            { key: 'departmentId', label: 'Department', type: 'select', lookup: '/api/admin/departments/lookup', col: 6 },
            { key: 'reportHeading', label: 'Report Heading', col: 8, help: 'Printed at the top of the lab report' }, F.sort, F.active],
    },
    TestItem: {
        title: 'Test Items', api: '/api/diag/test-items', size: 'modal-lg',
        filter: { key: 'testGroupId', label: 'Test Group', lookup: '/api/diag/test-groups/lookup' },
        columns: [C.code, C.name, { key: 'shortName', label: 'Short' }, { key: 'testGroup.name', label: 'Group' },
            { key: 'price', label: 'Price', type: 'money' }, { key: 'resultType', label: 'Result Type' }, { key: 'specimen.name', label: 'Specimen' },
            { key: 'isDiscountAllowed', label: 'Disc.', type: 'bool' }, C.active],
        fields: [F.code, F.name, { key: 'shortName', label: 'Short Name', col: 4 },
            { key: 'testGroupId', label: 'Test Group', type: 'select', lookup: '/api/diag/test-groups/lookup', required: true, col: 4 },
            { key: 'specimenId', label: 'Specimen', type: 'select', lookup: '/api/diag/specimens/lookup', col: 4 },
            { key: 'price', label: 'Price', type: 'number', required: true, col: 3, default: 0 },
            { key: 'vatPercent', label: 'VAT %', type: 'number', col: 3, default: 0 },
            { key: 'maxDiscountPercent', label: 'Max Discount %', type: 'number', col: 3, default: 100 },
            { key: 'resultType', label: 'Result Type', type: 'enum', options: ['Parameter', 'Text', 'Radiology'], col: 3, default: 'Parameter' },
            { key: 'isDiscountAllowed', label: 'Discount allowed', type: 'check', col: 3, default: true },
            { key: 'requiresSample', label: 'Needs sample', type: 'check', col: 3, default: true },
            { key: 'isOutsource', label: 'Outsource', type: 'check', col: 3 }, F.active,
            { key: 'defaultReportText', label: 'Default report text (Text / Radiology)', type: 'textarea', rows: 6, col: 12 },
            F.sort],
    },
    ReportParameter: {
        title: 'Report Parameters', api: '/api/diag/report-parameters', size: 'modal-lg', pageSize: 200,
        sub: 'Result lines of a test with unit and reference range',
        filter: { key: 'testItemId', label: 'Test Item', lookup: '/api/diag/test-items/lookup', required: true },
        columns: [{ key: 'sortOrder', label: 'Sort' }, { key: 'groupHeading', label: 'Heading' }, C.name, { key: 'unit', label: 'Unit' },
            { key: 'normalRange', label: 'Normal Range' }, { key: 'lowValue', label: 'Low' }, { key: 'highValue', label: 'High' },
            { key: 'inputType', label: 'Input' }, C.active],
        fields: [{ key: 'testItemId', label: 'Test Item', type: 'select', lookup: '/api/diag/test-items/lookup', required: true, col: 8, fromFilter: true },
            { key: 'sortOrder', label: 'Sort Order', type: 'number', col: 4, default: 0 },
            { key: 'name', label: 'Parameter Name', required: true, col: 8 }, { key: 'unit', label: 'Unit', col: 4 },
            { key: 'groupHeading', label: 'Group Heading', col: 6, help: 'e.g. DIFFERENTIAL COUNT' }, { key: 'method', label: 'Method', col: 6 },
            { key: 'normalRange', label: 'Normal Range (print text)', col: 12 },
            { key: 'normalRangeMale', label: 'Range – Male', col: 6 }, { key: 'normalRangeFemale', label: 'Range – Female', col: 6 },
            { key: 'lowValue', label: 'Low value (flag L)', type: 'number', col: 3 }, { key: 'highValue', label: 'High value (flag H)', type: 'number', col: 3 },
            { key: 'inputType', label: 'Input Type', type: 'enum', options: ['Numeric', 'Text', 'Option'], col: 3, default: 'Numeric' },
            { key: 'defaultValue', label: 'Default Value', col: 3 },
            { key: 'options', label: 'Options (comma separated, for Option type)', col: 9 }, F.active],
    },
    Specimen: { title: 'Specimens', api: '/api/diag/specimens', columns: [C.code, C.name, C.sort, C.active], fields: [F.code, F.name, F.sort, F.active] },
    Vacutainer: {
        title: 'Vacutainers', api: '/api/diag/vacutainers', sub: 'Sample tubes (colour is printed on the sticker)',
        columns: [C.code, C.name, { key: 'colorName', label: 'Colour', render: r => `<span class="dot" style="background:${App.esc(r.colorHex)}"></span> ${App.esc(r.colorName)}` }, C.sort, C.active],
        fields: [F.code, F.name, { key: 'colorName', label: 'Colour Name', col: 4 }, { key: 'colorHex', label: 'Colour', type: 'color', col: 4, default: '#7B1FA2' }, F.sort, F.active],
    },
    Machine: {
        title: 'Machines', api: '/api/diag/machines', sub: 'Lab analysers',
        columns: [C.code, C.name, { key: 'model', label: 'Model' }, { key: 'manufacturer', label: 'Manufacturer' }, { key: 'interfaceCode', label: 'Interface' }, C.active],
        fields: [F.code, F.name, { key: 'model', label: 'Model', col: 4 }, { key: 'manufacturer', label: 'Manufacturer', col: 4 },
            { key: 'interfaceCode', label: 'Interface Code', col: 4 }, F.sort, F.active],
    },
    VacuMachineMapping: {
        title: 'Vacu And Machine Group Mapping', api: '/api/diag/vacu-machine-mappings',
        sub: 'Which tube and analyser a test group uses. A row with a test item overrides its group.',
        filter: { key: 'testGroupId', label: 'Test Group', lookup: '/api/diag/test-groups/lookup' },
        columns: [{ key: 'testGroup.name', label: 'Test Group' }, { key: 'testItem.name', label: 'Test Item (override)' },
            { key: 'vacutainer.name', label: 'Vacutainer', render: r => `<span class="dot" style="background:${App.esc(r.vacutainer?.colorHex)}"></span> ${App.esc(r.vacutainer?.name)}` },
            { key: 'machine.name', label: 'Machine' }, { key: 'sampleVolume', label: 'Volume' }, C.active],
        fields: [{ key: 'testGroupId', label: 'Test Group', type: 'select', lookup: '/api/diag/test-groups/lookup', required: true, col: 6 },
            { key: 'testItemId', label: 'Test Item (optional)', type: 'select', lookup: '/api/diag/test-items/lookup', col: 6 },
            { key: 'vacutainerId', label: 'Vacutainer', type: 'select', lookup: '/api/diag/vacutainers/lookup', required: true, col: 6 },
            { key: 'machineId', label: 'Machine', type: 'select', lookup: '/api/diag/machines/lookup', col: 6 },
            { key: 'sampleVolume', label: 'Sample Volume', col: 6 }, F.active],
    },
    ReportDeliveryTime: {
        title: 'Report Delivery Time Set', api: '/api/diag/report-delivery-times',
        sub: 'Hours after billing. Test item row wins over test group row; otherwise the global default is used.',
        columns: [{ key: 'testGroup.name', label: 'Test Group' }, { key: 'testItem.name', label: 'Test Item' },
            { key: 'deliveryHours', label: 'Hours' }, { key: 'deliveryAt', label: 'Fixed Time' }, { key: 'remarks', label: 'Remarks' }, C.active],
        fields: [{ key: 'testGroupId', label: 'Test Group', type: 'select', lookup: '/api/diag/test-groups/lookup', col: 6 },
            { key: 'testItemId', label: 'Test Item', type: 'select', lookup: '/api/diag/test-items/lookup', col: 6 },
            { key: 'deliveryHours', label: 'Delivery after (hours)', type: 'number', required: true, col: 4, default: 24 },
            { key: 'deliveryAt', label: 'Fixed clock time (optional)', type: 'time', col: 4 }, F.active,
            { key: 'remarks', label: 'Remarks', col: 12 }],
    },
    LetterHead: {
        title: 'Letter Head Setup', api: '/api/diag/letter-heads', size: 'modal-lg',
        columns: [C.code, C.name, { key: 'title', label: 'Title' }, { key: 'tenant.name', label: 'Tenant' }, { key: 'isDefault', label: 'Default', type: 'bool' }, C.active],
        fields: [F.code, F.name, { key: 'tenantId', label: 'Tenant', type: 'select', lookup: '/api/admin/tenants/lookup', col: 6 },
            { key: 'isDefault', label: 'Default letter head', type: 'check', col: 3 }, F.active,
            { key: 'title', label: 'Title', col: 6 }, { key: 'subTitle', label: 'Sub Title', col: 6 },
            { key: 'address', label: 'Address', col: 12 }, { key: 'phone', label: 'Phone', col: 6 }, { key: 'email', label: 'Email', col: 6 },
            { key: 'logoPath', label: 'Logo file path (on report server)', col: 8 },
            { key: 'prePrintedTopMarginMm', label: 'Pre-printed pad top margin (mm)', type: 'number', col: 4, default: 45 },
            { key: 'footerText', label: 'Footer Text', type: 'textarea', col: 12 }],
    },
    Doctor: {
        title: 'Doctors', api: '/api/diag/doctors', size: 'modal-lg',
        columns: [C.code, C.name, { key: 'degree', label: 'Degree' }, { key: 'phone', label: 'Phone' },
            { key: 'isReferrer', label: 'Referrer', type: 'bool' }, { key: 'isReportSigner', label: 'Signs Report', type: 'bool' },
            { key: 'commissionPercent', label: 'Comm. %' }, C.active],
        fields: [F.code, F.name, { key: 'degree', label: 'Degree', col: 6 }, { key: 'designation', label: 'Designation', col: 6 },
            { key: 'phone', label: 'Phone', col: 4 }, { key: 'commissionPercent', label: 'Commission %', type: 'number', col: 4, default: 0 }, F.sort,
            { key: 'isReferrer', label: 'Referring doctor', type: 'check', col: 4, default: true },
            { key: 'isConsultant', label: 'Consultant', type: 'check', col: 4 },
            { key: 'isReportSigner', label: 'Signs lab reports', type: 'check', col: 4 }, F.active],
    },
    Media: {
        title: 'Media', api: '/api/diag/medias', sub: 'Agents who bring patients and earn commission',
        columns: [C.code, C.name, { key: 'phone', label: 'Phone' }, { key: 'organization', label: 'Organization' },
            { key: 'defaultCommissionPercent', label: 'Default %' }, C.active],
        fields: [F.code, F.name, { key: 'phone', label: 'Phone', col: 4 }, { key: 'organization', label: 'Organization', col: 8 },
            { key: 'defaultCommissionPercent', label: 'Default commission %', type: 'number', col: 4, default: 0,
                help: 'Used for tests that have no Media Commission Setup rule' },
            F.sort, F.active, { key: 'address', label: 'Address', type: 'textarea', col: 12 }],
    },
    MediaCommissionSetup: {
        title: 'Media Commission Setup', api: '/api/diag/media-commissions',
        sub: 'Test wise rule wins over test group rule; a rule for one media wins over a rule for all media; otherwise the media default % applies.',
        filter: { key: 'mediaId', label: 'Media', lookup: '/api/diag/medias/lookup' },
        columns: [{ key: 'media.name', label: 'Media', render: r => App.esc(r.media?.name || 'All media') },
            { key: 'testGroup.name', label: 'Test Group' }, { key: 'testItem.name', label: 'Test (test wise)' },
            { key: 'commissionType', label: 'Type' }, { key: 'value', label: 'Value' }, { key: 'remarks', label: 'Remarks' }, C.active],
        fields: [{ key: 'mediaId', label: 'Media (empty = all media)', type: 'select', lookup: '/api/diag/medias/lookup', col: 12 },
            { key: 'testGroupId', label: 'Test Group (group wise)', type: 'select', lookup: '/api/diag/test-groups/lookup', col: 6 },
            { key: 'testItemId', label: 'Test Item (test wise)', type: 'select', lookup: '/api/diag/test-items/lookup', col: 6,
                help: 'Choose a test for a test wise rule, or only a group for a group wise rule' },
            { key: 'commissionType', label: 'Type', type: 'enum', options: ['Percent', 'Flat'], col: 4, default: 'Percent' },
            { key: 'value', label: 'Value', type: 'number', required: true, col: 4, default: 0 }, F.active,
            { key: 'remarks', label: 'Remarks', col: 12 }],
    },
    DiscountSetup: {
        title: 'Discount Setup', api: '/api/diag/discount-setups',
        sub: 'Test wise: choose a test. Group wise: choose a test group. Leave both empty for all tests.',
        columns: [C.code, C.name, { key: 'discountType', label: 'Type' }, { key: 'value', label: 'Value' },
            { key: 'testGroup.name', label: 'Test Group' }, { key: 'testItem.name', label: 'Test' }, { key: 'doctor.name', label: 'Doctor' },
            { key: 'validFrom', label: 'From', type: 'date' }, { key: 'validTo', label: 'To', type: 'date' }, C.active],
        fields: [F.code, F.name, { key: 'discountType', label: 'Type', type: 'enum', options: ['Percent', 'Flat'], col: 4, default: 'Percent' },
            { key: 'value', label: 'Value', type: 'number', required: true, col: 4, default: 0 },
            { key: 'testGroupId', label: 'Only for Test Group (group wise)', type: 'select', lookup: '/api/diag/test-groups/lookup', col: 6 },
            { key: 'testItemId', label: 'Only for Test (test wise)', type: 'select', lookup: '/api/diag/test-items/lookup', col: 6 },
            { key: 'doctorId', label: 'Only for patients of Doctor', type: 'select', lookup: '/api/diag/doctors/lookup', col: 6 },
            { key: 'validFrom', label: 'Valid From', type: 'date', col: 4 }, { key: 'validTo', label: 'Valid To', type: 'date', col: 4 }, F.active],
    },
    OpdPackage: {
        title: 'OPD Packages', api: '/api/diag/opd-packages', size: 'modal-lg',
        sub: 'Sum of item prices must equal the package price',
        columns: [C.code, C.name, { key: 'packagePrice', label: 'Price', type: 'money' },
            { key: 'items', label: 'Tests', render: r => (r.items || []).map(i => App.esc(i.testItem?.name)).join(', ') },
            { key: 'validFrom', label: 'From', type: 'date' }, { key: 'validTo', label: 'To', type: 'date' }, C.active],
        fields: [F.code, F.name, { key: 'packagePrice', label: 'Package Price', type: 'number', required: true, col: 4, default: 0 },
            { key: 'validFrom', label: 'Valid From', type: 'date', col: 4 }, { key: 'validTo', label: 'Valid To', type: 'date', col: 4 },
            { key: 'description', label: 'Description', col: 12 }, F.sort, F.active],
        children: {
            key: 'items', label: 'Tests in package', total: 'itemPrice',
            fields: [{ key: 'testItemId', label: 'Test', type: 'select', lookup: '/api/diag/test-items/lookup', required: true },
                { key: 'itemPrice', label: 'Allocated Price', type: 'number', required: true, default: 0 }],
        },
    },
    Patient: {
        title: 'Patients', api: '/api/diag/patients',
        columns: [{ key: 'patientCode', label: 'Patient ID' }, C.name, { key: 'gender', label: 'Sex' }, { key: 'dateOfBirth', label: 'DOB', type: 'date' },
            { key: 'phone', label: 'Phone' }, { key: 'address', label: 'Address' }, { key: 'bloodGroup', label: 'Blood' }],
        fields: [{ key: 'name', label: 'Name', required: true, col: 8 }, { key: 'gender', label: 'Gender', type: 'enum', options: GENDER, col: 4, default: 'Male' },
            { key: 'dateOfBirth', label: 'Date of Birth', type: 'date', col: 4 }, { key: 'phone', label: 'Phone', col: 4 },
            { key: 'bloodGroup', label: 'Blood Group', type: 'enum', options: ['', 'A+', 'A-', 'B+', 'B-', 'AB+', 'AB-', 'O+', 'O-'], col: 4 },
            { key: 'email', label: 'Email', col: 6 },
            { key: 'religionId', label: 'Religion', type: 'select', lookup: '/api/admin/religions/lookup', col: 3 },
            { key: 'countryId', label: 'Country', type: 'select', lookup: '/api/admin/countries/lookup', col: 3 },
            { key: 'address', label: 'Address', type: 'textarea', col: 12 }, F.active],
    },
};

// =====================================================================================================
const MasterPage = (() => {
    const get = (obj, path) => path.split('.').reduce((o, k) => o?.[k], obj);

    function cell(col, row) {
        if (col.render) return col.render(row);
        const v = get(row, col.key);
        switch (col.type) {
            case 'bool': return yes(v);
            case 'money': return `<span class="money d-block">${App.money(v)}</span>`;
            case 'date': return App.date(v);
            case 'datetime': return App.dateTime(v);
            default: return App.esc(v);
        }
    }

    async function input(f, value, prefix = '') {
        const id = prefix + f.key;
        const v = value ?? '';
        switch (f.type) {
            case 'textarea': return `<textarea class="form-control" id="${id}" rows="${f.rows || 2}">${App.esc(v)}</textarea>`;
            case 'number': return `<input type="number" step="any" class="form-control" id="${id}" value="${App.esc(v)}">`;
            case 'date': return `<input type="date" class="form-control" id="${id}" value="${App.esc(String(v).slice(0, 10))}">`;
            case 'time': return `<input type="time" class="form-control" id="${id}" value="${App.esc(String(v).slice(0, 5))}">`;
            case 'color': return `<input type="color" class="form-control form-control-color w-100" id="${id}" value="${App.esc(v || '#000000')}">`;
            case 'password': return `<input type="password" class="form-control" id="${id}" autocomplete="new-password">`;
            case 'check': return `<div class="form-check form-switch mt-1"><input class="form-check-input" type="checkbox" id="${id}" ${v ? 'checked' : ''}></div>`;
            case 'enum': return `<select class="form-select" id="${id}">${f.options.map(o => `<option ${o === v ? 'selected' : ''}>${App.esc(o)}</option>`).join('')}</select>`;
            case 'select': {
                const items = await App.lookup(f.lookup);
                return `<select class="form-select" id="${id}">${App.options(items, v)}</select>`;
            }
            case 'multiselect': {
                const items = await App.lookup(f.lookup);
                const set = new Set((value || []).map(String));
                return `<div class="d-flex flex-wrap gap-3 border rounded p-2" id="${id}">${items.map(i => `
                    <label class="form-check mb-0"><input class="form-check-input" type="checkbox" value="${i.id}" ${set.has(String(i.id)) ? 'checked' : ''}>
                    <span class="form-check-label">${App.esc(i.name)}</span></label>`).join('')}</div>`;
            }
            default: return `<input class="form-control" id="${id}" value="${App.esc(v)}">`;
        }
    }

    function read(f, el) {
        switch (f.type) {
            case 'check': return el.checked;
            case 'number': return el.value === '' ? null : Number(el.value);
            case 'select': return el.value === '' ? null : Number(el.value);
            case 'multiselect': return [...el.querySelectorAll('input:checked')].map(i => Number(i.value));
            case 'date': return el.value || null;
            case 'time': return el.value ? (el.value.length === 5 ? el.value + ':00' : el.value) : null;
            case 'password': return el.value || null;
            case 'enum': return el.value === '' ? null : el.value;
            default: return el.value.trim() === '' ? null : el.value.trim();
        }
    }

    async function init(key) {
        const cfg = MASTERS[key];
        const root = document.getElementById('masterRoot');
        if (!cfg) { root.innerHTML = `<div class="empty">Unknown setup screen "${App.esc(key)}".</div>`; return; }
        document.title = cfg.title + ' - ' + APP.appName;
        const rights = await App.rights();
        const state = { page: 1, pageSize: cfg.pageSize || 20, search: '', filter: '' };

        root.innerHTML = `
            <div class="page-head">
                <div><h1>${App.esc(cfg.title)}</h1>${cfg.sub ? `<div class="sub">${App.esc(cfg.sub)}</div>` : ''}</div>
                <div class="ms-auto d-flex gap-2 flex-wrap">
                    ${cfg.filter ? `<select class="form-select form-select-sm" style="min-width:220px" id="mFilter"></select>` : ''}
                    <input class="form-control form-control-sm" style="width:220px" placeholder="Search…" id="mSearch">
                    ${rights.canAdd ? '<button class="btn btn-primary btn-sm" id="mAdd"><i class="bi bi-plus-lg me-1"></i>Add New</button>' : ''}
                </div>
            </div>
            <div class="card"><div class="table-responsive"><table class="table table-hover table-sm align-middle">
                <thead><tr><th style="width:44px">#</th>${cfg.columns.map(c => `<th>${App.esc(c.label)}</th>`).join('')}<th style="width:90px"></th></tr></thead>
                <tbody id="mBody"></tbody></table></div>
                <div class="card-footer d-flex align-items-center small text-muted" id="mFoot"></div></div>`;

        if (cfg.filter) {
            const items = await App.lookup(cfg.filter.lookup);
            const sel = document.getElementById('mFilter');
            sel.innerHTML = App.options(items, '', cfg.filter.required ? `-- Select ${cfg.filter.label} --` : `All ${cfg.filter.label}s`);
            const fromUrl = new URLSearchParams(location.search).get(cfg.filter.key);
            if (fromUrl) sel.value = fromUrl;
            sel.onchange = () => { state.filter = sel.value; state.page = 1; load(); };
            state.filter = sel.value;
        }
        document.getElementById('mSearch').oninput = App.debounce(e => { state.search = e.target.value; state.page = 1; load(); });
        document.getElementById('mAdd')?.addEventListener('click', () => edit(null));

        async function load() {
            const body = document.getElementById('mBody');
            if (cfg.filter?.required && !state.filter) {
                body.innerHTML = `<tr><td colspan="99" class="empty">Select a ${App.esc(cfg.filter.label)} to see its records.</td></tr>`;
                document.getElementById('mFoot').innerHTML = '';
                return;
            }
            body.innerHTML = '<tr><td colspan="99" class="empty">Loading…</td></tr>';
            try {
                const q = { page: state.page, pageSize: state.pageSize, search: state.search };
                if (cfg.filter) q[cfg.filter.key] = state.filter;
                const res = await App.api(cfg.api + App.qs(q));
                const start = (res.page - 1) * res.pageSize;
                body.innerHTML = res.items.length ? res.items.map((r, i) => `<tr>
                    <td class="text-muted">${start + i + 1}</td>
                    ${cfg.columns.map(c => `<td>${cell(c, r)}</td>`).join('')}
                    <td class="text-end text-nowrap">
                        ${rights.canEdit ? `<button class="btn btn-sm btn-outline-primary py-0" data-edit="${r.id}" title="Edit"><i class="bi bi-pencil"></i></button>` : ''}
                        ${rights.canDelete ? `<button class="btn btn-sm btn-outline-danger py-0" data-del="${r.id}" title="Delete"><i class="bi bi-trash"></i></button>` : ''}
                    </td></tr>`).join('')
                    : '<tr><td colspan="99" class="empty">No record found.</td></tr>';
                const pages = Math.max(1, Math.ceil(res.total / res.pageSize));
                document.getElementById('mFoot').innerHTML = `<span>${res.total} record(s)</span>
                    <div class="ms-auto btn-group btn-group-sm">
                        <button class="btn btn-outline-secondary" data-page="${res.page - 1}" ${res.page <= 1 ? 'disabled' : ''}>‹</button>
                        <span class="btn btn-outline-secondary disabled">${res.page} / ${pages}</span>
                        <button class="btn btn-outline-secondary" data-page="${res.page + 1}" ${res.page >= pages ? 'disabled' : ''}>›</button></div>`;
            } catch (e) {
                body.innerHTML = `<tr><td colspan="99" class="empty text-danger">${App.esc(e.message)}</td></tr>`;
            }
        }

        root.addEventListener('click', async e => {
            const b = e.target.closest('button');
            if (!b) return;
            if (b.dataset.page) { state.page = Number(b.dataset.page); load(); }
            if (b.dataset.edit) edit(Number(b.dataset.edit));
            if (b.dataset.del && await App.confirmBox('Delete this record?', 'Delete')) {
                await App.run(async () => { await App.api(`${cfg.api}/${b.dataset.del}`, { method: 'DELETE' }); App.toast('Deleted.'); load(); });
            }
        });

        async function edit(id) {
            const entity = id ? await App.run(() => App.api(`${cfg.api}/${id}`)) : {};
            if (id && !entity) return;
            if (!id) {
                cfg.fields.forEach(f => { if (f.default !== undefined) entity[f.key] = f.default; });
                cfg.fields.filter(f => f.fromFilter && state.filter).forEach(f => entity[f.key] = Number(state.filter));
                if (cfg.filter && state.filter && entity[cfg.filter.key] === undefined) entity[cfg.filter.key] = Number(state.filter);
            }
            const fieldsHtml = (await Promise.all(cfg.fields.map(async f => `
                <div class="col-md-${f.col || 6} ${f.type === 'check' ? 'd-flex flex-column' : ''}">
                    <label class="form-label ${f.required ? 'required' : ''}" for="f_${f.key}">${App.esc(f.label)}</label>
                    ${await input(f, entity[f.key], 'f_')}
                    ${f.help ? `<div class="form-text">${App.esc(f.help)}</div>` : ''}
                </div>`))).join('');

            const childHtml = cfg.children ? `<hr><div class="d-flex align-items-center mb-2"><h6 class="mb-0 fw-bold">${App.esc(cfg.children.label)}</h6>
                <span class="ms-3 small text-muted" id="childTotal"></span>
                <button type="button" class="btn btn-sm btn-outline-primary ms-auto" id="addChild"><i class="bi bi-plus"></i> Add row</button></div>
                <div class="table-responsive"><table class="table table-sm"><thead><tr>${cfg.children.fields.map(f => `<th>${App.esc(f.label)}</th>`).join('')}<th></th></tr></thead>
                <tbody id="childBody"></tbody></table></div>` : '';

            const m = App.modal((id ? 'Edit ' : 'Add ') + cfg.title.replace(/s$/, ''), `<form class="row g-3" id="mForm">${fieldsHtml}</form>${childHtml}`, {
                size: cfg.size || '', static: true,
                footer: '<button class="btn btn-light" data-bs-dismiss="modal">Cancel</button><button class="btn btn-primary" id="mSave"><i class="bi bi-check2 me-1"></i>Save</button>',
            });

            if (cfg.children) {
                const tbody = m.q('#childBody');
                const updateTotal = () => {
                    if (!cfg.children.total) return;
                    const sum = [...tbody.querySelectorAll(`[data-k="${cfg.children.total}"]`)].reduce((s, i) => s + Number(i.value || 0), 0);
                    m.q('#childTotal').textContent = 'Total: ' + App.money(sum);
                };
                const addRow = async row => {
                    const tr = document.createElement('tr');
                    tr.dataset.id = row.id || 0;
                    tr.innerHTML = (await Promise.all(cfg.children.fields.map(async f => {
                        const html = await input(f, row[f.key] ?? f.default, '');
                        return `<td>${html.replace(/ id="[^"]*"/, ` data-k="${f.key}"`)}</td>`;
                    }))).join('') + '<td><button type="button" class="btn btn-sm btn-outline-danger py-0" data-rm><i class="bi bi-x"></i></button></td>';
                    tr.querySelectorAll('.form-control,.form-select').forEach(i => i.classList.add('form-control-sm'));
                    tr.querySelector('[data-rm]').onclick = () => { tr.remove(); updateTotal(); };
                    tr.addEventListener('input', updateTotal);
                    tbody.appendChild(tr);
                    updateTotal();
                };
                for (const r of entity[cfg.children.key] || []) await addRow(r);
                m.q('#addChild').onclick = () => addRow({});
            }

            m.q('#mSave').onclick = e => App.run(async () => {
                const model = { ...entity };
                for (const f of cfg.fields) {
                    const el = m.q('#f_' + f.key);
                    const v = read(f, el);
                    if (f.required && (v === null || v === '')) { el.classList.add('is-invalid'); el.focus(); throw new Error(`${f.label} is required.`); }
                    model[f.key] = v;
                }
                if (cfg.children) {
                    model[cfg.children.key] = [...m.qa('#childBody tr')].map(tr => {
                        const row = { id: Number(tr.dataset.id), isActive: true };
                        for (const f of cfg.children.fields) {
                            const el = tr.querySelector(`[data-k="${f.key}"]`);
                            const v = read(f, el);
                            if (f.required && (v === null || v === '')) throw new Error(`${f.label} is required in every row.`);
                            row[f.key] = v;
                        }
                        return row;
                    });
                }
                // Navigation objects are for display only – never send them back.
                Object.keys(model).forEach(k => { if (model[k] && typeof model[k] === 'object' && !Array.isArray(model[k])) delete model[k]; });
                await App.api(id ? `${cfg.api}/${id}` : cfg.api, { method: id ? 'PUT' : 'POST', body: model });
                m.close();
                App.toast('Saved successfully.');
                load();
            }, e.currentTarget);
        }

        load();
    }

    return { init };
})();
