(() => {
  // ----- Confirmation dialog: <form data-confirm="Message" data-confirm-title="…" data-confirm-action="Send"> -----
  const dialog = document.getElementById("confirmDialog");
  let pending = null;

  document.addEventListener("submit", (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.dataset.confirm || form.dataset.confirmed === "1" || !dialog) return;
    event.preventDefault();
    pending = { form, submitter: event.submitter };
    dialog.querySelector("[data-confirm-title]").textContent = form.dataset.confirmTitle || dialog.dataset.defaultTitle || "Are you sure?";
    dialog.querySelector("[data-confirm-message]").textContent = form.dataset.confirm;
    const ok = dialog.querySelector("[data-confirm-ok]");
    ok.textContent = form.dataset.confirmAction || dialog.dataset.defaultAction || "Confirm";
    ok.classList.toggle("btn--danger", form.dataset.confirmTone === "danger");
    ok.classList.toggle("btn--primary", form.dataset.confirmTone !== "danger");
    dialog.returnValue = "";
    dialog.showModal();
    ok.focus();
  }, true);

  dialog?.addEventListener("close", () => {
    if (dialog.returnValue === "ok" && pending) {
      const { form, submitter } = pending;
      form.dataset.confirmed = "1";
      if (submitter && submitter.form === form) form.requestSubmit(submitter); else form.requestSubmit();
    }
    pending = null;
  });

  // ----- Double-submit guard: once a valid form submits, its buttons show a busy state. -----
  document.addEventListener("submit", (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || event.defaultPrevented || ["get", "dialog"].includes(form.method.toLowerCase())) return;
    if (form.dataset.submitting === "1") {
      event.preventDefault();
      return;
    }
    form.dataset.submitting = "1";
    const buttons = form.querySelectorAll("button[type=submit], button:not([type])");
    if (event.submitter) event.submitter.classList.add("is-busy");
    // Disable on the next tick so the clicked button's name/value is still part of the submission.
    setTimeout(() => buttons.forEach((b) => { b.disabled = true; }), 0);
  });

  window.addEventListener("pageshow", (event) => {
    if (!event.persisted) return;
    document.querySelectorAll("form[data-submitting]").forEach((form) => {
      delete form.dataset.submitting;
      delete form.dataset.confirmed;
      form.querySelectorAll("button").forEach((b) => { b.disabled = false; b.classList.remove("is-busy"); });
    });
  });

  // ----- Toasts: dismiss button, auto-hide for success/info. -----
  document.querySelectorAll("[data-toast]").forEach((toast) => {
    const close = () => toast.remove();
    toast.querySelector("[data-toast-close]")?.addEventListener("click", close);
    if (toast.dataset.toast === "success" || toast.dataset.toast === "info") setTimeout(close, 7000);
  });

  // ----- Tables: labels for the stacked mobile layout, and quick filtering. -----
  document.querySelectorAll("table.data-table").forEach((table) => {
    const headers = [...table.querySelectorAll("thead th")].map((th) => th.textContent.trim());
    table.querySelectorAll("tbody tr").forEach((row) => {
      [...row.children].forEach((cell, index) => {
        if (!cell.hasAttribute("data-label") && headers[index]) cell.setAttribute("data-label", headers[index]);
      });
    });
  });

  document.querySelectorAll("[data-table-filter]").forEach((input) => {
    const table = document.querySelector(input.dataset.tableFilter);
    if (!table) return;
    const rows = [...table.querySelectorAll("tbody tr:not(.empty-row)")];
    const noMatch = table.querySelector("tbody .no-match-row");
    input.addEventListener("input", () => {
      const query = input.value.trim().toLowerCase();
      let visible = 0;
      rows.forEach((row) => {
        if (row.classList.contains("no-match-row")) return;
        const show = !query || row.textContent.toLowerCase().includes(query);
        row.hidden = !show;
        if (show) visible++;
      });
      if (noMatch) noMatch.hidden = visible > 0 || !query;
    });
  });

  // ----- Filter bars: submit GET forms as soon as a select changes. -----
  document.querySelectorAll("select[data-autosubmit]").forEach((select) => {
    select.addEventListener("change", () => select.form?.requestSubmit());
  });

  // ----- Billing period inputs: picking a start date fills the end with the end of that month. -----
  document.querySelectorAll("[data-period-start]").forEach((start) => {
    const end = start.form?.querySelector("[data-period-end]");
    if (!end) return;
    start.addEventListener("change", () => {
      if (!start.value) return;
      const [y, m] = start.value.split("-").map(Number);
      const last = new Date(Date.UTC(y, m, 0)).getUTCDate();
      const endOfMonth = `${start.value.slice(0, 7)}-${String(last).padStart(2, "0")}`;
      if (!end.value || end.value < start.value) end.value = endOfMonth;
    });
  });
  // ----- Charts: one tooltip per column listing every series, on hover and keyboard focus. -----
  document.querySelectorAll(".viz-plot").forEach((plot) => {
    const tooltip = plot.querySelector(".viz-tooltip");
    if (!tooltip) return;
    const show = (col) => {
      const data = col.querySelector(".viz-tip-data");
      if (!data) return;
      tooltip.replaceChildren(...[...data.children].map((node) => node.cloneNode(true)));
      tooltip.hidden = false;
      const plotBox = plot.getBoundingClientRect();
      const colBox = col.getBoundingClientRect();
      const center = colBox.left - plotBox.left + colBox.width / 2;
      const width = tooltip.offsetWidth;
      const left = center + width + 16 > plotBox.width ? center - width - 12 : center + 12;
      tooltip.style.left = `${Math.max(0, left)}px`;
      tooltip.style.top = `${Math.max(0, colBox.height - tooltip.offsetHeight) / 3}px`;
    };
    const hide = () => { tooltip.hidden = true; };
    plot.querySelectorAll(".viz-col").forEach((col) => {
      col.addEventListener("pointerenter", () => show(col));
      col.addEventListener("focus", () => show(col));
      col.addEventListener("pointerleave", hide);
      col.addEventListener("blur", hide);
    });
  });
  // ----- Select previews: show the chosen option's data-sample text elsewhere on the page. -----
  document.querySelectorAll("select[data-preview]").forEach((select) => {
    const target = document.querySelector(select.dataset.preview);
    if (!target) return;
    select.addEventListener("change", () => {
      target.textContent = select.selectedOptions[0]?.dataset.sample ?? "";
    });
  });
  // ----- Split rule form: method switch, share rows, running total, resident prefill. -----
  document.querySelectorAll("[data-rule-form]").forEach((form) => {
    const sharesSection = form.querySelector("[data-shares]");
    const body = form.querySelector("[data-share-rows]");
    const template = form.querySelector("[data-share-template]");
    const totalCell = form.querySelector("[data-share-total]");
    const addressSelect = form.querySelector("[data-rule-address]");
    const validFrom = form.querySelector("[data-rule-valid-from]");
    const isNew = form.hasAttribute("data-rule-new");
    const currency = form.dataset.currency || "";
    const locale = document.documentElement.lang || undefined;
    if (!sharesSection || !body || !template) return;

    const method = () => form.querySelector("input[name='Rule.Method']:checked")?.value;
    const rows = () => [...body.querySelectorAll("tr[data-share-row]")];

    const reindex = () => {
      rows().forEach((row, index) => {
        row.querySelectorAll("[name^='Rule.Shares[']").forEach((input) => {
          input.name = input.name.replace(/Rule\.Shares\[\d+\]/, `Rule.Shares[${index}]`);
          if (input.id) input.id = input.id.replace(/Rule_Shares_\d+__/, `Rule_Shares_${index}__`);
        });
        row.querySelectorAll("[data-valmsg-for^='Rule.Shares[']").forEach((span) => {
          span.dataset.valmsgFor = span.dataset.valmsgFor.replace(/Rule\.Shares\[\d+\]/, `Rule.Shares[${index}]`);
        });
      });
    };

    const update = () => {
      const current = method();
      sharesSection.hidden = current === "Equal";
      form.querySelectorAll("[data-method-help]").forEach((help) => { help.hidden = help.dataset.methodHelp !== current; });
      form.querySelectorAll("[data-share-suffix]").forEach((suffix) => { suffix.textContent = current === "Percentage" ? "%" : currency; });
      form.querySelectorAll("[data-share-value]").forEach((input) => {
        input.max = current === "Percentage" ? "100" : "";
        input.step = "0.01";
      });
      if (!totalCell) return;
      const values = rows()
        .filter((row) => row.querySelector("select")?.value)
        .map((row) => parseFloat(row.querySelector("[data-share-value]")?.value || "0") || 0);
      const total = values.reduce((sum, value) => sum + value, 0);
      if (current === "Percentage") {
        const ok = Math.abs(total - 100) < 0.005;
        totalCell.textContent = `${total.toLocaleString(locale, { maximumFractionDigits: 2 })} %`;
        totalCell.className = `num ${ok ? "shares-total--ok" : "shares-total--warning"}`;
        totalCell.title = ok ? "" : totalCell.dataset.warningTitle || "";
      } else {
        totalCell.textContent = `${total.toLocaleString(locale, { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${currency}`.trim();
        totalCell.className = "num";
        totalCell.title = "";
      }
    };

    const addRow = (contactId = "") => {
      const fragment = template.content.cloneNode(true);
      const row = fragment.querySelector("tr");
      body.appendChild(fragment);
      if (contactId) row.querySelector("select").value = contactId;
      reindex();
      update();
      return row;
    };

    form.addEventListener("change", (event) => {
      if (event.target.matches("input[name='Rule.Method']")) update();
    });
    form.addEventListener("input", (event) => {
      if (event.target.matches("[data-share-value], select")) update();
    });
    form.addEventListener("click", (event) => {
      const remove = event.target.closest("[data-share-remove]");
      if (remove) {
        remove.closest("tr")?.remove();
        if (rows().length === 0) addRow();
        reindex();
        update();
      }
      if (event.target.closest("[data-share-add]")) addRow().querySelector("select")?.focus();
    });

    // New rules: picking an address fills the rows with the people living there on the start date.
    const prefill = async () => {
      if (!isNew || !addressSelect?.value || !form.dataset.residentsUrl) return;
      const untouched = rows().every((row) => !row.querySelector("[data-share-value]")?.value);
      if (!untouched) return;
      const url = `${form.dataset.residentsUrl}?addressId=${encodeURIComponent(addressSelect.value)}&date=${encodeURIComponent(validFrom?.value || "")}`;
      try {
        const response = await fetch(url, { headers: { Accept: "application/json" } });
        if (!response.ok) return;
        const residents = await response.json();
        rows().forEach((row) => row.remove());
        residents.forEach((resident) => addRow(resident.id));
        if (residents.length === 0) addRow();
      } catch { /* keep the rows as they are */ }
    };
    addressSelect?.addEventListener("change", prefill);
    validFrom?.addEventListener("change", prefill);

    update();
  });
})();
