const assessmentForm = document.querySelector("[data-assessment-form]");
const assessmentError = document.querySelector("#assessment-error");
const assessmentComplete = document.querySelector("#assessment-complete");

if (!window.kawachTracking.getIdentity().assessmentId) {
  window.location.replace("/");
}

assessmentForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  assessmentError.hidden = true;

  const submitButton = assessmentForm.querySelector("button[type='submit']");
  const step = document.querySelector("[data-assessment-step]");
  const questions = assessmentForm.querySelectorAll("[data-question-code]");

  submitButton.disabled = true;
  submitButton.textContent = "Saving...";

  try {
    for (const question of questions) {
      const selectedAnswer = question.querySelector("input:checked");

      const module = question.closest("[data-assessment-module]");

      if (!module) {
        throw new Error(`Module information is missing for ${question.dataset.questionCode}.`);
      }

      await window.kawachTracking.submitAnswer({
        stepCode: step.dataset.assessmentStep,
        stepName: step.dataset.stepName,
        moduleCode: module.dataset.moduleCode,
        moduleName: module.dataset.moduleName,
        questionCode: question.dataset.questionCode,
        questionText: question.dataset.questionText,
        answerCode: selectedAnswer
          ? selectedAnswer.dataset.answerCode
          : question.dataset.unselectedAnswerCode,
        answerText: selectedAnswer
          ? selectedAnswer.value
          : question.dataset.unselectedAnswerText
      });
    }

    await window.kawachTracking.trackStep(
      step.dataset.assessmentStep,
      "Completed",
      { reason: "form-submit" });

    const identity = window.kawachTracking.getIdentity();
    const response = await fetch("/api/assessment/complete", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        userId: identity.userId,
        assessmentId: identity.assessmentId
      })
    });

    if (!response.ok) {
      const problem = await response.json();
      throw new Error(problem.detail || problem.title || "Unable to complete assessment.");
    }

    assessmentForm.hidden = true;
    assessmentComplete.hidden = false;
  } catch (error) {
    assessmentError.textContent = error.message;
    assessmentError.hidden = false;
  } finally {
    submitButton.disabled = false;
    submitButton.textContent = "Save and complete";
  }
});
