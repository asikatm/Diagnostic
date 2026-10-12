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
    $('media').innerHTML = App.options(lookups.medias || [], '', 'None');
    $('media').onchange = () => $('mediaInfo').textContent = $('media').value
        ? 'Commission is calculated from Media Commission Setup when the invoice is saved.' : '';
    $('package').innerHTML = App.options(packages.map(p => ({ id: p.id, name: `${p.name} (${App.money(p.packagePrice)})` })), '', '-- OPD package (optional) --');
    $('discountSetup').innerHTML = App.options(discounts.map(d => ({ id: d.id, name: `${d.name}` })), '', 'None');
    $('payMode').innerHTML = App.options(lookups.paymentModes, lookups.paymentModes.find(m => m.code === 'CASH')?.id, null);
    $('autoPrint').checked = App.store.get('diag.autoPrintReceipt') ?? true;
    $('autoPrint').onchange = e => App.store.set('diag.autoPrintReceipt', e.target.checked);

    function fillChannels() {
        const mode = lookups.paymentModes.find(m => String(m.id) === $('payMode').value);
        const ch = lookups.paymentChannels.filter(c => String(c.paymentModeId) === $('payMode').value);
        $('payChannel').innerHTML = App.options(ch, '', ch.length ? '-- Select --' : 'N/A');
        $('payChannel').disabled = ch.length === 0;
        $('payRefLabel').classList.toggle('required', !!mode?.requiresReference);
    }
    $('payMode').onchange = fillChannels;
    fillChannels();

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
            $('pName').value = p.name; $('pGender').value = p.gender; $('pPhone').value = p.phone || ''; $('pAddress').value = p.address || '';
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
        t => `<span><b>${App.esc(t.name)}</b> <span class="text-muted small">${App.esc(t.code)} · ${App.esc(t.group)}</span></span><span class="money">${App.money(t.price)}</span>`,
        t => {
            $('testSearch').value = '';
            if (state.lines.some(l => l.id === t.id)) { App.toast(`${t.name} is already added.`, 'warning'); return; }
            state.lines.push({ id: t.id, name: t.name, price: t.price, vatPercent: t.vatPercent, groupId: t.testGroupId,
                allowed: t.isDiscountAllowed, maxPct: t.maxDiscountPercent, manual: 0 });
            render();
        });

    $('package').onchange = () => {
        state.pkg = packages.find(p => String(p.id) === $('package').value) || null;
        state.lines = state.lines.filter(l => !l.fromPackage);
        if (state.pkg) {
            const clash = state.lines.filter(l => state.pkg.items.some(i => i.testItemId === l.id));
            state.lines = state.lines.filter(l => !clash.includes(l));
            if (clash.length) App.toast(`Removed tests already in the package: ${clash.map(c => c.name).join(', ')}`, 'warning');
            state.lines.unshift(...state.pkg.items.map(i => ({ id: i.testItemId, name: i.testName, price: i.itemPrice, vatPercent: i.vatPercent,
                groupId: i.testGroupId, fromPackage: true, allowed: false, maxPct: 0, manual: 0 })));
        }
        render();
    };

    function calculate() {
        const lines = state.lines.map(l => ({ ...l, max: l.fromPackage || !l.allowed ? 0 : round2(l.price * l.maxPct / 100), disc: Math.min(l.manual, l.price) }));
        const spread = (targets, amount) => {
            const room = targets.reduce((s, l) => s + l.max - l.disc, 0);
            amount = Math.min(amount, room);
            const cand = targets.filter(l => l.max - l.disc > 0);
            const basis = cand.reduce((s, l) => s + l.price, 0);
            let remaining = amount;
            cand.forEach((l, i) => {
                if (remaining <= 0) return;
                let share = i === cand.length - 1 ? remaining : round2(amount * l.price / basis);
                share = Math.min(share, remaining, l.max - l.disc);
                l.disc = round2(l.disc + share); remaining = round2(remaining - share);
            });
            for (const l of targets) { if (remaining <= 0) break; const add = Math.min(remaining, l.max - l.disc); l.disc = round2(l.disc + add); remaining = round2(remaining - add); }
            return room;
        };
        const setup = discounts.find(d => String(d.id) === $('discountSetup').value);
        if (setup) {
            const eligible = lines.filter(l => (!setup.testItemId || l.id === setup.testItemId) && (!setup.testGroupId || l.groupId === setup.testGroupId));
            if (setup.discountType === 'Percent') eligible.forEach(l => l.disc = Math.min(l.max, round2(l.disc + l.price * setup.value / 100)));
            else spread(eligible, setup.value);
        }
        const extra = Number($('extraDiscount').value || 0);
        const room = lines.reduce((s, l) => s + l.max - l.disc, 0);
        if (extra > 0) spread(lines, extra);
        lines.forEach(l => { l.vat = round2((l.price - l.disc) * (l.vatPercent || 0) / 100); l.net = round2(l.price - l.disc + l.vat); });
        return { lines, extraRoom: room, setup };
    }

    function render() {
        const { lines, extraRoom } = calculate();
        $('lines').innerHTML = lines.length ? lines.map((l, i) => `<tr>
            <td class="text-muted">${i + 1}</td>
            <td>${App.esc(l.name)} ${l.fromPackage ? '<span class="chip ms-1">Package</span>' : ''}</td>
            <td class="money">${App.money(l.price)}</td>
            <td class="text-end">${l.fromPackage || !l.allowed ? `<span class="money">${App.money(l.disc)}</span>`
                : `<input type="number" min="0" max="${l.max}" class="form-control form-control-sm text-end" data-disc="${i}" value="${l.manual || ''}" placeholder="${App.money(l.disc)}">`}</td>
            <td class="money fw-semibold">${App.money(l.net)}</td>
            <td class="text-end">${l.fromPackage ? '' : `<button class="btn btn-sm btn-outline-danger py-0" data-rm="${i}"><i class="bi bi-x"></i></button>`}</td>
        </tr>`).join('') : '<tr><td colspan="6" class="empty">Search and add tests, or choose a package.</td></tr>';

        const sum = k => round2(lines.reduce((s, l) => s + l[k], 0));
        const net = sum('net');
        $('tGross').textContent = App.money(sum('price'));
        $('tDiscount').textContent = App.money(sum('disc'));
        $('tVat').textContent = App.money(sum('vat'));
        $('tNet').textContent = App.money(net);
        $('extraDiscount').max = extraRoom;
        if (!$('payAmount').dataset.touched) $('payAmount').value = net;
        $('tDue').textContent = App.money(Math.max(0, net - Number($('payAmount').value || 0)));
        state.net = net;
    }

    $('lines').addEventListener('click', e => {
        const b = e.target.closest('[data-rm]');
        if (b) { state.lines.splice(Number(b.dataset.rm), 1); render(); }
    });
    $('lines').addEventListener('change', e => {
        const inp = e.target.closest('[data-disc]');
        if (!inp) return;
        const line = state.lines[Number(inp.dataset.disc)];
        const max = line.allowed ? round2(line.price * line.maxPct / 100) : 0;
        let v = Number(inp.value || 0);
        if (v > max) { App.toast(`Maximum discount for ${line.name} is ${App.money(max)}.`, 'warning'); v = max; }
        line.manual = v;
        render();
    });
    $('discountSetup').onchange = render;
    $('extraDiscount').oninput = App.debounce(render, 300);
    $('payAmount').oninput = () => { $('payAmount').dataset.touched = '1'; $('tDue').textContent = App.money(Math.max(0, state.net - Number($('payAmount').value || 0))); };

    // ------------------------------------------------------------ save
    function clearAll() {
        setPatient(null);
        state.lines = []; state.pkg = null;
        $('package').value = ''; $('discountSetup').value = ''; $('extraDiscount').value = 0; $('referDoctor').value = ''; $('media').value = ''; $('mediaInfo').textContent = '';
        $('remarks').value = ''; $('payRef').value = ''; delete $('payAmount').dataset.touched;
        render();
        $('pName').focus();
    }
    $('btnClear').onclick = clearAll;

    $('btnSave').onclick = e => App.run(async () => {
        if (!state.patient && !$('pName').value.trim()) { $('pName').focus(); throw new Error('Patient name is required.'); }
        if (state.lines.length === 0) throw new Error('Add at least one test.');
        const amount = Number($('payAmount').value || 0);
        if (amount > state.net + 0.001) throw new Error('Paid amount cannot be more than the net payable.');

        const dto = {
            patientId: state.patient?.id ?? null,
            patient: state.patient ? null : {
                name: $('pName').value.trim(), gender: $('pGender').value, phone: $('pPhone').value.trim() || null,
                address: $('pAddress').value.trim() || null, dateOfBirth: dobFromAge(),
            },
            patientAge: ageText(),
            referDoctorId: Number($('referDoctor').value) || null,
            mediaId: Number($('media').value) || null,
            opdPackageId: state.pkg?.id ?? null,
            discountSetupId: Number($('discountSetup').value) || null,
            tests: state.lines.filter(l => !l.fromPackage).map(l => ({ testItemId: l.id, discountAmount: l.manual || 0 })),
            extraDiscount: Number($('extraDiscount').value || 0),
            payment: amount > 0 ? { amount, paymentModeId: Number($('payMode').value), paymentChannelId: Number($('payChannel').value) || null, transactionRef: $('payRef').value.trim() || null } : null,
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
    }, e.currentTarget);

    document.addEventListener('keydown', e => { if (e.key === 'F9') { e.preventDefault(); $('btnSave').click(); } });
    render();
    $('pName').focus();
});
