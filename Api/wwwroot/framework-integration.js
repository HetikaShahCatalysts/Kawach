(function () {
  const pageSteps = {
    "page-risk": { code: "RISK_ASSESSMENT", name: "Risk Assessment" },
    "page-obs": { code: "OBSERVATION_SCORING", name: "Observation Scoring" },
    "page-decision": { code: "PATHWAY_DECISION", name: "Pathway Decision" },
    "wf-page": { code: "WORKFLOW_RESPONSE", name: "Workflow / Response" },
    "page-monitoring": { code: "MONITORING", name: "Monitoring" },
    "page-closure": { code: "CASE_CLOSURE", name: "Case Closure" },
    "page-framework": { code: "FRAMEWORK", name: "Framework" }
  };

  const identity = {
    userId: Number(sessionStorage.getItem("userId")) || null,
    assessmentId: sessionStorage.getItem("assessmentId"),
    languageCode: document.documentElement.lang || "en"
  };

  const originalGoToPage = window.goToPage;
  const originalResetAll = window.resetAll;
  let navigationInProgress = false;
  let requestedPage = null;
  let requestedStep = null;

  installParticipantDialog();

  window.goToPage = async function (pageId, step) {
    if (navigationInProgress) return;

    if (pageId === "page-risk" && !hasIdentity()) {
      requestedPage = pageId;
      requestedStep = step;
      showParticipantDialog();
      return;
    }

    navigationInProgress = true;
    try {
      const previousPage = window.currentPageId;
      if (hasIdentity() && pageSteps[previousPage]) {
        await syncSelections(previousPage);
        await trackStep(pageSteps[previousPage], "Completed");
      }

      originalGoToPage(pageId, step);

      if (hasIdentity() && pageSteps[pageId]) {
        await trackStep(pageSteps[pageId], "Started");
      }
    } catch (error) {
      showTrackingError(error);
    } finally {
      navigationInProgress = false;
    }
  };

  window.resetAll = function () {
    sessionStorage.removeItem("userId");
    sessionStorage.removeItem("assessmentId");
    sessionStorage.removeItem("participantName");
    identity.userId = null;
    identity.assessmentId = null;
    originalResetAll();
  };

  async function syncSelections(pageId) {
    if (pageId !== "page-risk" && pageId !== "page-obs") return;

    const step = pageSteps[pageId];
    const selectedAnswers = pageId === "page-risk"
      ? collectRiskSelections()
      : collectObservationSelections();

    await postJson("/api/assessment/answers/sync", {
      userId: identity.userId,
      assessmentId: identity.assessmentId,
      languageCode: identity.languageCode,
      stepCode: step.code,
      stepName: step.name,
      selectedAnswers
    });
  }

  function collectRiskSelections() {
    const modules = [
      ["HOUSEHOLD_FACTORS", "Household Factors", window.HH_FACTORS, window.appState.hhChecked],
      ["CHILD_FACTORS", "Child Factors", window.CH_FACTORS, window.appState.chChecked],
      ["COMMUNITY_FACTORS", "Community Factors", window.CO_FACTORS, window.appState.coChecked],
      ["PROTECTIVE_FACTORS", "Protective Factors", window.PROTECTIVE_FACTORS, window.appState.pfChecked]
    ];

    return collectSelectedFromModules(modules);
  }

  function collectObservationSelections() {
    const selected = [];

    window.OBS_BUCKETS.forEach(bucket => {
      bucket.items.forEach(question => {
        const stateKey = window.obsStateKey(question.pathway);
        if (!window.appState[stateKey][question.id]) return;

        selected.push(toSelectedAnswer(
          normalizeCode(bucket.id),
          bucket.title,
          question));
      });
    });

    return selected;
  }

  function collectSelectedFromModules(modules) {
    const selected = [];

    modules.forEach(([moduleCode, moduleName, questions, checked]) => {
      questions.forEach(question => {
        if (!checked[question.id]) return;
        selected.push(toSelectedAnswer(moduleCode, moduleName, question));
      });
    });

    return selected;
  }

  function toSelectedAnswer(moduleCode, moduleName, question) {
    return {
      moduleCode,
      moduleName,
      questionCode: question.code || normalizeCode(question.id),
      questionText: question.text,
      answerCode: "SELECTED",
      answerText: "Selected",
      score: Number(question.pts || 0)
    };
  }

  async function trackStep(step, eventType) {
    await postJson("/api/assessment/track-step", {
      userId: identity.userId,
      assessmentId: identity.assessmentId,
      stepCode: step.code,
      eventType,
      pageVersion: "framework-16062026",
      clientOccurredOn: new Date().toISOString(),
      metadata: null
    });
  }

  async function postJson(path, body) {
    const response = await fetch(path, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body)
    });

    if (!response.ok) {
      const problem = await response.json().catch(() => ({}));
      throw new Error(problem.detail || problem.title || "Unable to save assessment data.");
    }

    return response.json();
  }

  function hasIdentity() {
    return Boolean(identity.userId && identity.assessmentId);
  }

  function normalizeCode(value) {
    return String(value || "")
      .trim()
      .replace(/[^a-zA-Z0-9]+/g, "_")
      .replace(/^_+|_+$/g, "")
      .toUpperCase();
  }

  function installParticipantDialog() {
    const dialog = document.createElement("dialog");
    dialog.id = "participant-dialog";
    dialog.innerHTML = `
      <form method="dialog" id="framework-participant-form" class="framework-participant-form">
        <div class="framework-modal-label">Before you begin</div>
        <h2>Participant details</h2>
        <p>These details create the User ID used to connect answers and step timings.</p>
        <label>Full name<input name="fullName" maxlength="150" required></label>
        <div class="framework-modal-grid">
          <label>Age<input name="age" type="number" min="1" max="120"></label>
          <label>Gender<input name="gender" maxlength="30"></label>
          <label>Phone<input name="phone" type="tel" maxlength="20"></label>
          <label>Email<input name="email" type="email" maxlength="254"></label>
        </div>
        <label>Location<input name="location" maxlength="200"></label>
        <div id="framework-participant-error" class="framework-modal-error" hidden></div>
        <div class="framework-modal-actions">
          <button type="button" data-close-participant>Cancel</button>
          <button type="submit">Save and start assessment</button>
        </div>
      </form>`;

    document.body.appendChild(dialog);

    const style = document.createElement("style");
    style.textContent = `
      #participant-dialog{width:min(92vw,620px);border:0;border-radius:14px;padding:0;box-shadow:0 24px 80px rgba(13,27,46,.3)}
      #participant-dialog::backdrop{background:rgba(13,27,46,.7)}
      .framework-participant-form{display:grid;gap:14px;padding:28px;font-family:var(--font,Arial,sans-serif)}
      .framework-participant-form h2{font-size:22px;margin:0}
      .framework-participant-form p{color:#718096;line-height:1.5}
      .framework-participant-form label{display:grid;gap:6px;font-weight:600;color:#0f1923}
      .framework-participant-form input{width:100%;padding:10px 12px;border:1px solid #cfd8e4;border-radius:7px;font:inherit}
      .framework-modal-grid{display:grid;grid-template-columns:1fr 1fr;gap:12px}
      .framework-modal-label{color:#1e5fa8;font-size:11px;font-weight:700;text-transform:uppercase;letter-spacing:1px}
      .framework-modal-actions{display:flex;justify-content:flex-end;gap:10px;margin-top:6px}
      .framework-modal-actions button{padding:10px 16px;border:1px solid #cfd8e4;border-radius:7px;background:#fff;cursor:pointer;font:inherit;font-weight:600}
      .framework-modal-actions button[type=submit]{background:#0d1b2e;color:#fff;border-color:#0d1b2e}
      .framework-modal-error{padding:10px;border-radius:7px;background:#fef2f2;color:#991b1b}
      @media(max-width:600px){.framework-modal-grid{grid-template-columns:1fr}}
    `;
    document.head.appendChild(style);

    dialog.querySelector("[data-close-participant]").addEventListener("click", () => dialog.close());
    dialog.querySelector("form").addEventListener("submit", createParticipant);
  }

  async function createParticipant(event) {
    event.preventDefault();
    const form = event.currentTarget;
    const submit = form.querySelector("button[type='submit']");
    const error = form.querySelector("#framework-participant-error");
    const values = new FormData(form);
    const age = values.get("age");

    submit.disabled = true;
    error.hidden = true;

    try {
      const assessment = await postJson("/api/assessment/start", {
        assessmentCode: "KAWACH_FRAMEWORK",
        languageCode: document.documentElement.lang || "en",
        participant: {
          fullName: values.get("fullName"),
          age: age ? Number(age) : null,
          gender: values.get("gender") || null,
          phone: values.get("phone") || null,
          email: values.get("email") || null,
          location: values.get("location") || null
        }
      });

      identity.userId = assessment.userId;
      identity.assessmentId = assessment.assessmentId;
      identity.languageCode = assessment.languageCode;
      sessionStorage.setItem("userId", String(identity.userId));
      sessionStorage.setItem("assessmentId", identity.assessmentId);
      sessionStorage.setItem("participantName", assessment.participant.fullName);

      document.querySelector("#participant-dialog").close();
      originalGoToPage(requestedPage || "page-risk", requestedStep || "risk");
      await trackStep(pageSteps["page-risk"], "Started");
    } catch (caught) {
      error.textContent = caught.message;
      error.hidden = false;
    } finally {
      submit.disabled = false;
    }
  }

  function showParticipantDialog() {
    document.querySelector("#participant-dialog").showModal();
  }

  function showTrackingError(error) {
    console.error(error);
    window.alert(`Assessment data could not be saved: ${error.message}`);
  }
})();
