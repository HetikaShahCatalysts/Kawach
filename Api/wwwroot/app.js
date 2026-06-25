const form = document.querySelector("#participant-form");
const errorMessage = document.querySelector("#error");
const success = document.querySelector("#success");
const backButton = document.querySelector("#back");

form.addEventListener("submit", async (event) => {
  event.preventDefault();
  errorMessage.hidden = true;

  const submitButton = form.querySelector("button[type='submit']");
  const fields = new FormData(form);
  const age = fields.get("age");

  const request = {
    assessmentCode: "KAWACH",
    languageCode: document.documentElement.lang || "en",
    participant: {
      fullName: fields.get("fullName"),
      age: age ? Number(age) : null,
      gender: fields.get("gender") || null,
      phone: fields.get("phone") || null,
      email: fields.get("email") || null,
      location: fields.get("location") || null
    }
  };

  submitButton.disabled = true;
  submitButton.textContent = "Saving...";

  try {
    const response = await fetch("/api/assessment/start", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request)
    });

    if (!response.ok) {
      const problem = await response.json();
      throw new Error(problem.detail || problem.title || "Unable to save the details.");
    }

    const assessment = await response.json();
    window.kawachTracking.configure(assessment);
    sessionStorage.setItem("participantName", assessment.participant.fullName);

    await window.kawachTracking.trackStep(
      "PARTICIPANT_DETAILS",
      "Started",
      null,
      false,
      window.kawachTracking.pageOpenedOn);
    await window.kawachTracking.trackStep("PARTICIPANT_DETAILS", "Completed");

    form.hidden = true;
    success.hidden = false;
  } catch (error) {
    errorMessage.textContent = error.message;
    errorMessage.hidden = false;
  } finally {
    submitButton.disabled = false;
    submitButton.textContent = "Continue to assessment";
  }
});

backButton.addEventListener("click", () => {
  sessionStorage.removeItem("userId");
  sessionStorage.removeItem("assessmentId");
  sessionStorage.removeItem("participantName");
  success.hidden = true;
  form.hidden = false;
});
