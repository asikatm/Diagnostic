/* Investigation Entry – billing screen. Totals are previewed with the same rules the API applies on save. */
'use strict';

App.ready(async () => {
    const $ = id => document.getElementById(id);
    const round2 = v => Math.round(v * 100) / 100;
    const state = { patient: null, lines: [], pkg: null };
    let lookups, packages, discounts;

    try {
        [lookups, packages, discounts] = await Promise.all([
            App.api('/api/diag/lookups/billing'), App.api('/api/diag/opd-packages/available'), App.api('/api/diag/discount-setups/available'),
        ]);
    } catch (e) { App.toast(e.message, 'danger'); return; }

    $('referDoctor').innerHTML = App.options(lookups.doctors, '', 'Self');
    $('area').innerHTML = App.options(lookups.marketingAreas || [], '', '-- Area --');

    // Header: today's collection of this user, discount permission, deposit slip.
    async function loadMyCollection() {
        try {
            const m = await App.api('/api/diag/investigations/my-collection');
            $('myCash').textContent = App.money(m.cash);
            state.maxDiscPct = m.maxDiscountPercent;
            state.userId = m.userId;
            if (m.maxDiscountPercent != null) {
                $('discLimit').textContent = `Your discount permission: up to ${m.maxDiscountPercent}% of the discountable amount.`;
                $('discLimit').classList.remove('d-none');
            }
        } catch { /* header info only */ }
    }
    $('btnDeposit').onclick = () => {
        const today = App.isoDate();
        App.report('/api/reports/run/USER_WISE_COLLECTION_SUMMARY' + App.qs({ FromDate: today, ToDate: today, UserId: state.userId }));
    };
    loadMyCollection();
    $('media').innerHTML = App.options(lookups.medias || [], '', 'None');
    $('media').onchange = () => $('mediaInfo').textContent = $('media').value
        ? 'Commission is calculated from Media Commission Setup when the invoice is saved.' : '';
    $('package').innerHTML = App.options(packages.map(p => ({ id: p.id, name: `${p.name} (${App.money(p.packagePrice)})` })), '', '-- OPD package (optional) --');
    $('discountSetup').innerHTML = App.options(discounts.map(d => ({ id: d.id, name: `${d.name}` })), '', 'None');
    $('autoPrint').checked = App.store.get('diag.autoPrintReceipt') ?? true;
    $('autoPrint').onchange = e => App.store.set('diag.autoPrintReceipt', e.target.checked);

    // Cash / card / mobile banking are taken together (split payment), like the counter in the training video.
    const modeByCode = code => lookups.paymentModes.find(m => m.code === code);
    const channels = code => lookups.paymentChannels.filter(c => c.paymentModeId === modeByCode(code)?.id);
    $('cardChannel').innerHTML = App.options(channels('CARD'), '', 'Select…');
    $('mobileChannel').innerHTML = App.options(channels('MFS'), '', 'Select…');
    if (!modeByCode('CARD')) ['cardChannel', 'payCard', 'cardRef'].forEach(id => $(id).disabled = true);
    if (!modeByCode('MFS')) ['mobileChannel', 'payMobile', 'mobileRef'].forEach(id => $(id).disabled = true);

    // ------------------------------------------------------------ autocomplete helper
    function autocomplete(input, box, fetcher, render, pick) {
        let items = [], active = -1;
        const close = () => { box.classList.add('d-none'); active = -1; };
        const paint = () => {
            box.innerHTML = items.length ? items.map((it, i) => `<div class="item ${i === active ? 'active' : ''}" data-i="${i}">${render(it)}</div>`).join('')
                : '<div class="item text-muted">No match</div>';
            box.classList.remove('d-none');
        };
        input.addEventListener('input', App.debounce(async () => {
            const q = input.value.trim();
            if (q.length < 2) { close(); return; }
            try { items = await fetcher(q); active = items.length ? 0 : -1; paint(); } catch (e) { App.toast(e.message, 'danger'); }
        }, 250));
        input.addEventListener('keydown', e => {
            if (box.classList.contains('d-none')) return;
            if (e.key === 'ArrowDown') { active = Math.min(items.length - 1, active + 1); paint(); e.preventDefault(); }
            else if (e.key === 'ArrowUp') { active = Math.max(0, active - 1); paint(); e.preventDefault(); }
            else if (e.key === 'Enter' && items[active]) { pick(items[active]); close(); e.preventDefault(); }
            else if (e.key === 'Escape') close();
        });
        box.addEventListener('mousedown', e => {
            const el = e.target.closest('[data-i]');
            if (el) { pick(items[Number(el.dataset.i)]); close(); }
        });
        input.addEventListener('blur', () => setTimeout(close, 150));
    }

    // ------------------------------------------------------------ patient
    autocomplete($('patientSearch'), $('patientResults'),
        q => App.api('/api/diag/patients/search' + App.qs({ q })),
        p => `<span><b>${App.esc(p.name)}</b> <span class="text-muted small">${App.esc(p.patientCode)}</span></span><span class="small text-muted">${App.esc(p.phone)} · ${App.esc(p.gender)}</span>`,
        p => setPatient(p));

    function setPatient(p) {
        state.patient = p;
        $('patientSearch').value = '';
        $('patientForm').querySelectorAll('input,select').forEach(i => i.disabled = !!p);
        $('btnNewPatient').classList.toggle('d-none', !p);
        $('patientInfo').classList.toggle('d-none', !p);
        if (p) {
            $('pTitle').value = p.title || ''; $('pName').value = p.name; $('pGender').value = p.gender; $('pPhone').value = p.phone || ''; $('pAddress').value = p.address || '';
            $('pDob').value = p.dateOfBirth || '';
            fillAgeFromDob();
            $('patientInfo').innerHTML = `Existing patient <b>${App.esc(p.patientCode)}</b>`;
        } else {
            ['pName', 'pPhone', 'pAddress', 'pDob', 'pAgeY', 'pAgeM', 'pAgeD'].forEach(id => $(id).value = '');
        }
    }
    $('btnNewPatient').onclick = () => setPatient(null);

    // ------------------------------------------------------------ age <-> date of birth
    // Typing Y / M / D fills the date of birth; picking a date of birth fills Y / M / D.
    const num = id => Math.max(0, Math.floor(Number($(id).value || 0)));

    function fillDobFromAge() {
        const y = num('pAgeY'), m = num('pAgeM'), d = num('pAgeD');
        if (!y && !m && !d) { $('pDob').value = ''; return; }
        const dob = new Date();
        dob.setFullYear(dob.getFullYear() - y);
        dob.setMonth(dob.getMonth() - m);
        dob.setDate(dob.getDate() - d);
        $('pDob').value = App.isoDate(dob);
    }

    function fillAgeFromDob() {
        ['pAgeY', 'pAgeM', 'pAgeD'].forEach(id => $(id).value = '');
        if (!$('pDob').value) return;
        const dob = new Date($('pDob').value + 'T00:00:00'), now = new Date();
        now.setHours(0, 0, 0, 0);
        if (dob > now) { App.toast('Date of birth cannot be in the future.', 'warning'); $('pDob').value = ''; return; }
        let y = now.getFullYear() - dob.getFullYear(), m = now.getMonth() - dob.getMonth(), d = now.getDate() - dob.getDate();
        if (d < 0) { m--; d += new Date(now.getFullYear(), now.getMonth(), 0).getDate(); } // days of previous month
        if (m < 0) { y--; m += 12; }
        $('pAgeY').value = y; $('pAgeM').value = m; $('pAgeD').value = d;
    }

    ['pAgeY', 'pAgeM', 'pAgeD'].forEach(id => $(id).addEventListener('input', fillDobFromAge));
    $('pDob').addEventListener('change', fillAgeFromDob);

    function ageText() {
        const y = num('pAgeY'), m = num('pAgeM'), d = num('pAgeD');
        if (!y && !m && !d) return null;
        // Adults: years only; children: years + months; infants: months + days.
        const parts = y >= 5 ? [`${y} Y`] : [y ? `${y} Y` : '', m ? `${m} M` : '', !y && d ? `${d} D` : ''];
        return parts.filter(Boolean).join(' ') || `${d} D`;
    }
    const dobFromAge = () => $('pDob').value || null;

    // ------------------------------------------------------------ tests
    autocomplete($('testSearch'), $('testResults'),
        q => App.api('/api/diag/test-items/search' + App.qs({ q })),
        t => `<span><b>${App.esc(t.name)}</b> <span class="text-muted small">${App.esc(t.code)} · ${App.esc(t.group)}</span>
                ${t.isFasting ? '<span class="chip text-danger ms-1">Fasting</span>' : ''}</span><span class="money">${App.money(t.price)}</span>`,
        t => {
            $('testSearch').value = '';
            if (state.lines.some(l => l.id === t.id)) { App.toast(`${t.name} is already added.`, 'warning'); return; }
            if (t.isFasting) App.toast(`${t.name} needs a fasting sample – please confirm with the patient.`, 'warning');
            state.lines.push({ id: t.id, name: t.name, unit: t.price, qty: 1, vatPercent: t.vatPercent, groupId: t.testGroupId,
                allowed: t.isDiscountAllowed, maxPct: t.maxDiscountPercent, discPct: 0, urgent: false, note: '',
                consumables: t.consumables || [] });
            render();
        });

    $('package').onchange = () => {
        state.pkg = packages.find(p => String(p.id) === $('package').value) || null;
        state.lines = state.lines.filter(l => !l.fromPackage);
        if (state.pkg) {
            const clash = state.lines.filter(l => state.pkg.items.some(i => i.testItemId === l.id));
            state.lines = state.lines.filter(l => !clash.includes(l));
            if (clash.length) App.toast(`Removed tests already in the package: ${clash.map(c => c.name).join(', ')}`, 'warning');
            state.lines.unshift(...state.pkg.items.map(i => ({ id: i.testItemId, name: i.testName, unit: i.itemPrice, qty: 1, vatPercent: i.vatPercent,
                groupId: i.testGroupId, fromPackage: true, allowed: false, maxPct: 0, discPct: 0, consumables: [] })));
        }
        render();
    };

    // Same rules as the API: line % discount → scheme → extra (flat, spread by amount). Consumables are never discounted.
    function calculate() {
        const rows = [];
        state.lines.forEach((l, idx) => {
            const amount = round2(l.unit * l.qty);
            const max = l.fromPackage || !l.allowed ? 0 : round2(amount * l.maxPct / 100);
            rows.push({ ...l, idx, amount, max, disc: Math.min(max, round2(amount * (l.discPct || 0) / 100)) });
            (l.consumables || []).forEach(c => rows.push({ name: c.name, unit: c.price, qty: c.quantity, amount: round2(c.price * c.quantity),
                vatPercent: c.vatPercent, max: 0, disc: 0, consumable: true, parentIdx: idx }));
        });
        const spread = (targets, amountToGive) => {
            const room = targets.reduce((s, l) => s + l.max - l.disc, 0);
            amountToGive = Math.min(amountToGive, room);
            const cand = targets.filter(l => l.max - l.disc > 0);
            const basis = cand.reduce((s, l) => s + l.amount, 0);
            let remaining = amountToGive;
            cand.forEach((l, i) => {
                if (remaining <= 0) return;
                let share = i === cand.length - 1 ? remaining : round2(amountToGive * l.amount / basis);
                share = Math.min(share, remaining, l.max - l.disc);
                l.disc = round2(l.disc + share); remaining = round2(remaining - share);
            });
            for (const l of targets) { if (remaining <= 0) break; const add = Math.min(remaining, l.max - l.disc); l.disc = round2(l.disc + add); remaining = round2(remaining - add); }
        };
        const tests = rows.filter(r => !r.consumable);
        const setup = discounts.find(d => String(d.id) === $('discountSetup').value);
        if (setup) {
            const eligible = tests.filter(l => (!setup.testItemId || l.id === setup.testItemId) && (!setup.testGroupId || l.groupId === setup.testGroupId));
            if (setup.discountType === 'Percent') eligible.forEach(l => l.disc = Math.min(l.max, round2(l.disc + l.amount * setup.value / 100)));
            else spread(eligible, setup.value);
        }
        const room = round2(tests.reduce((s, l) => s + l.max - l.disc, 0));
        const extra = Number($('extraDiscount').value || 0);
        if (extra > 0) spread(tests, extra);
        rows.forEach(l => { l.vat = round2((l.amount - l.disc) * (l.vatPercent || 0) / 100); l.net = round2(l.amount - l.disc + l.vat); });
        const discountable = round2(tests.filter(l => l.max > 0).reduce((s, l) => s + l.amount, 0));
        return { rows, extraRoom: room, discountable };
    }

    const paid = () => ['payCash', 'payCard', 'payMobile'].reduce((s, id) => s + Number($(id).value || 0), 0);

    function render() {
        const { rows, extraRoom, discountable } = calculate();
        let sl = 0;
        $('lines').innerHTML = rows.length ? rows.map(l => l.consumable ? `<tr class="text-muted small">
                <td></td><td class="ps-3"><i class="bi bi-arrow-return-right me-1"></i>${App.esc(l.name)} <span class="chip">auto</span></td>
                <td>${l.qty}</td><td class="money">${App.money(l.unit)}</td><td></td><td></td><td></td>
                <td class="money">${App.money(l.net)}</td><td></td></tr>`
            : `<tr>
                <td class="text-muted">${++sl}</td>
                <td>${App.esc(l.name)} ${l.fromPackage ? '<span class="chip ms-1">Package</span>' : ''}</td>
                <td>${l.fromPackage ? 1 : `<input type="number" min="1" max="100" class="form-control form-control-sm px-1" data-qty="${l.idx}" value="${l.qty}">`}</td>
                <td class="money">${App.money(l.unit)}</td>
                <td>${l.fromPackage || !l.allowed ? `<span class="small text-muted">${App.money(l.disc)}</span>`
                    : `<input type="number" min="0" max="${l.maxPct}" class="form-control form-control-sm px-1" data-pct="${l.idx}" value="${l.discPct || 0}" title="Max ${l.maxPct}%">`}</td>
                <td>${l.fromPackage ? '' : `<input class="form-control form-control-sm" data-note="${l.idx}" value="${App.esc(l.note || '')}">`}</td>
                <td class="text-center">${l.fromPackage ? '' : `<input class="form-check-input" type="checkbox" data-urgent="${l.idx}" ${l.urgent ? 'checked' : ''}>`}</td>
                <td class="money fw-semibold">${App.money(l.net)}</td>
                <td class="text-end">${l.fromPackage ? '' : `<button class="btn btn-sm btn-outline-danger py-0" data-rm="${l.idx}"><i class="bi bi-trash"></i></button>`}</td>
            </tr>`).join('') : '<tr><td colspan="9" class="empty">Search and add tests, or choose a package.</td></tr>';

        const sum = k => round2(rows.reduce((s, l) => s + l[k], 0));
        const net = sum('net'), disc = sum('disc');
        $('tGross').textContent = App.money(sum('amount'));
        $('tDiscountable').textContent = App.money(discountable);
        $('tDiscount').textContent = App.money(disc);
        $('tVat').textContent = App.money(sum('vat'));
        $('tNet').textContent = App.money(net);
        $('extraDiscount').max = extraRoom;
        state.net = net; state.discountable = discountable; state.discount = disc;
        if (!state.payTouched) $('payCash').value = Math.max(0, round2(net - Number($('payCard').value || 0) - Number($('payMobile').value || 0)));
        updatePaid();
    }
    function updatePaid() {
        $('tPaid').textContent = App.money(paid());
        $('tDue').textContent = App.money(Math.max(0, state.net - paid()));
        const needRemarks = state.discount > 0;
        $('remarksHint').classList.toggle('d-none', !needRemarks || !!$('remarks').value.trim());
        $('remarks').classList.toggle('is-invalid', needRemarks && !$('remarks').value.trim());
    }

    $('lines').addEventListener('click', e => {
        const b = e.target.closest('[data-rm]');
        if (b) { state.lines.splice(Number(b.dataset.rm), 1); render(); }
    });
    $('lines').addEventListener('change', e => {
        const t = e.target, d = t.dataset;
        if (d.qty !== undefined) state.lines[d.qty].qty = Math.min(100, Math.max(1, Math.floor(Number(t.value) || 1)));
        else if (d.pct !== undefined) {
            const line = state.lines[d.pct];
            let v = Math.max(0, Number(t.value) || 0);
            if (v > line.maxPct) { App.toast(`Maximum discount for ${line.name} is ${line.maxPct}%.`, 'warning'); v = line.maxPct; }
            line.discPct = v;
        }
        else if (d.note !== undefined) { state.lines[d.note].note = t.value; return; }
        else if (d.urgent !== undefined) { state.lines[d.urgent].urgent = t.checked; return; }
        render();
    });
    $('discountSetup').onchange = render;
    // Disc(%) works on the discountable amount and fills Disc(Tk); Disc(Tk) clears the percent.
    $('extraPct').oninput = App.debounce(() => {
        const pct = Math.min(100, Math.max(0, Number($('extraPct').value || 0)));
        $('extraDiscount').value = round2(state.discountable * pct / 100);
        render();
    }, 300);
    $('extraDiscount').oninput = App.debounce(() => { $('extraPct').value = 0; render(); }, 300);
    // Cash follows "net − card − mobile" until the user types a cash amount himself.
    $('payCash').addEventListener('input', () => { state.payTouched = true; updatePaid(); });
    ['payCard', 'payMobile'].forEach(id => $(id).addEventListener('input', () => {
        if (!state.payTouched)
            $('payCash').value = Math.max(0, round2(state.net - Number($('payCard').value || 0) - Number($('payMobile').value || 0)));
        updatePaid();
    }));
    $('remarks').addEventListener('input', updatePaid);

    // ------------------------------------------------------------ save
    function clearAll() {
        setPatient(null);
        state.lines = []; state.pkg = null; state.payTouched = false;
        $('package').value = ''; $('discountSetup').value = ''; $('extraDiscount').value = 0; $('extraPct').value = 0;
        $('referDoctor').value = ''; $('media').value = ''; $('mediaInfo').textContent = ''; $('area').value = ''; $('pTitle').value = '';
        ['remarks', 'cardRef', 'mobileRef'].forEach(id => $(id).value = '');
        ['payCard', 'payMobile'].forEach(id => $(id).value = 0);
        render();
        $('pName').focus();
    }
    $('btnClear').onclick = clearAll;

    function paymentRows() {
        const rows = [];
        const add = (amount, mode, channelId, ref, label) => {
            if (amount <= 0) return;
            if (!mode) throw new Error(`Payment mode for ${label} is not set up (Administrative → Payment Mode).`);
            if (mode.requiresReference && !ref) throw new Error(`Reference is required for ${label} payment.`);
            rows.push({ amount, paymentModeId: mode.id, paymentChannelId: channelId || null, transactionRef: ref || null });
        };
        add(Number($('payCash').value || 0), modeByCode('CASH'), null, null, 'cash');
        add(Number($('payCard').value || 0), modeByCode('CARD'), Number($('cardChannel').value), $('cardRef').value.trim(), 'card');
        add(Number($('payMobile').value || 0), modeByCode('MFS'), Number($('mobileChannel').value), $('mobileRef').value.trim(), 'mobile');
        return rows;
    }

    $('btnSave').onclick = e => App.run(async () => {
        if (!state.patient && !$('pName').value.trim()) { $('pName').focus(); throw new Error('Patient name is required.'); }
        if (state.lines.length === 0) throw new Error('Add at least one test.');
        if (paid() > state.net + 0.001) throw new Error('Paid amount cannot be more than the net payable.');
        if (state.maxDiscPct != null && state.discountable > 0 && state.discount * 100 / state.discountable > state.maxDiscPct + 0.001)
            throw new Error(`Your discount permission is ${state.maxDiscPct}% (${App.money(state.discountable * state.maxDiscPct / 100)} Tk). This bill gives ${App.money(state.discount)} Tk.`);
        if (state.discount > 0 && !$('remarks').value.trim()) { $('remarks').focus(); throw new Error('Remarks is required when any discount is applied.'); }

        const dto = {
            patientId: state.patient?.id ?? null,
            patient: state.patient ? null : {
                title: $('pTitle').value || null, name: $('pName').value.trim(), gender: $('pGender').value, phone: $('pPhone').value.trim() || null,
                address: $('pAddress').value.trim() || null, dateOfBirth: dobFromAge(),
            },
            patientAge: ageText(),
            referDoctorId: Number($('referDoctor').value) || null,
            mediaId: Number($('media').value) || null,
            marketingAreaId: Number($('area').value) || null,
            opdPackageId: state.pkg?.id ?? null,
            discountSetupId: Number($('discountSetup').value) || null,
            tests: state.lines.filter(l => !l.fromPackage).map(l => ({ testItemId: l.id, quantity: l.qty, discountPercent: l.discPct || 0,
                isUrgent: !!l.urgent, note: (l.note || '').trim() || null })),
            extraDiscount: Number($('extraDiscount').value || 0),
            payments: paymentRows(),
            remarks: $('remarks').value.trim() || null,
        };

        const res = await App.api('/api/diag/investigations', { method: 'POST', body: dto });
        const receiptUrl = `/api/reports/money-receipt/${res.investigationId}` + App.qs({ paymentId: res.paymentId });
        if ($('autoPrint').checked) App.report(receiptUrl);

        const m = App.modal('Invoice saved', `
            <div class="text-center">
                <i class="bi bi-check-circle-fill text-success" style="font-size:2.5rem"></i>
                <div class="fs-5 fw-bold mt-2">${App.esc(res.invoiceNo)}</div>
                <div class="text-muted">Net ${App.money(res.netAmount)} · Due <b class="text-danger">${App.money(res.dueAmount)}</b></div>
                ${res.receiptNo ? `<div class="small text-muted">Money receipt ${App.esc(res.receiptNo)}</div>` : ''}
            </div>`, {
            footer: `<button class="btn btn-outline-primary" data-print><i class="bi bi-printer me-1"></i>Receipt</button>
                     <a class="btn btn-outline-primary" href="/Diagnostic/SampleCollection?invoiceNo=${encodeURIComponent(res.invoiceNo)}"><i class="bi bi-droplet me-1"></i>Sample Collection</a>
                     <button class="btn btn-primary" data-bs-dismiss="modal">New Bill</button>`,
        });
        m.q('[data-print]').onclick = () => App.report(receiptUrl);
        m.el.addEventListener('hidden.bs.modal', clearAll);
        loadMyCollection();
    }, e.currentTarget);

    document.addEventListener('keydown', e => { if (e.key === 'F9') { e.preventDefault(); $('btnSave').click(); } });
    render();
    $('pName').focus();
});
