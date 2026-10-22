(() => {
  // ----- Confirmation dialog: <form data-confirm="Message" data-confirm-title="…" data-confirm-action="Send"> -----
  const dialog = document.getElementById("confirmDialog");
  let pending = null;

  document.addEventListener("submit", (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.dataset.confirm || form.dataset.confirmed === "1" || !dialog) return;
    event.preventDefault();
    pending = { form, submitter: event.submitter };
    dialog.querySelector("[data-confirm-title]").textContent = form.dataset.confirmTitle || "Are you sure?";
    dialog.querySelector("[data-confirm-message]").textContent = form.dataset.confirm;
    const ok = dialog.querySelector("[data-confirm-ok]");
    ok.textContent = form.dataset.confirmAction || "Confirm";
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
})();
