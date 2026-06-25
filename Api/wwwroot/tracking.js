(function () {
  const state = {
    userId: Number(sessionStorage.getItem("userId")) || null,
    assessmentId: sessionStorage.getItem("assessmentId"),
    languageCode: document.documentElement.lang || "en"
  };
  const pageOpenedOn = new Date().toISOString();
  let currentStepCompleted = false;

  async function post(path, body, keepalive = false) {
    const response = await fetch(path, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
      keepalive
    });

    if (!response.ok) {
      const problem = await response.json().catch(() => ({}));
      throw new Error(problem.detail || problem.title || "Tracking request failed.");
    }

    return response.json();
  }

  function requireIdentity() {
    if (!state.userId || !state.assessmentId) {
      throw new Error("Participant details must be completed before tracking begins.");
    }
  }

  async function trackStep(
    stepCode,
    eventType,
    metadata,
    keepalive = false,
    clientOccurredOn = new Date().toISOString()) {
    requireIdentity();
    const response = await post("/api/assessment/track-step", {
      userId: state.userId,
      assessmentId: state.assessmentId,
      stepCode,
      eventType,
      pageVersion: document.documentElement.dataset.pageVersion || null,
      clientOccurredOn,
      metadata: metadata || null
    }, keepalive);

    if (eventType === "Completed") {
      currentStepCompleted = true;
    }

    return response;
  }

  async function submitAnswer(answer) {
    requireIdentity();
    return post("/api/assessment/answer", {
      userId: state.userId,
      assessmentId: state.assessmentId,
      languageCode: state.languageCode,
      stepCode: answer.stepCode,
      stepName: answer.stepName,
      moduleCode: answer.moduleCode,
      moduleName: answer.moduleName,
      questionCode: answer.questionCode,
      questionText: answer.questionText,
      answerCode: answer.answerCode || null,
      answerText: answer.answerText,
      score: Number(answer.score || answer.scoreDelta || 0),
      metadata: answer.metadata || null
    });
  }

  function configure(identity) {
    state.userId = Number(identity.userId);
    state.assessmentId = identity.assessmentId;
    state.languageCode = identity.languageCode || state.languageCode;
    sessionStorage.setItem("userId", String(state.userId));
    sessionStorage.setItem("assessmentId", state.assessmentId);
  }

  const step = document.querySelector("[data-assessment-step]");
  if (step && state.userId && state.assessmentId) {
    const stepCode = step.dataset.assessmentStep;
    trackStep(stepCode, "Started").catch(console.error);

    window.addEventListener("pagehide", () => {
      if (!currentStepCompleted) {
        trackStep(stepCode, "Completed", { reason: "pagehide" }, true).catch(() => {});
      }
    }, { once: true });
  }

  window.kawachTracking = {
    configure,
    trackStep,
    submitAnswer,
    pageOpenedOn,
    getIdentity: () => ({ ...state })
  };
})();
