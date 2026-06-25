const loginForm = document.querySelector("#admin-login-form");
const loginError = document.querySelector("#login-error");

loginForm.addEventListener("submit", async event => {
  event.preventDefault();
  loginError.hidden = true;

  const submitButton = loginForm.querySelector("button[type='submit']");
  const values = new FormData(loginForm);
  submitButton.disabled = true;
  submitButton.textContent = "Signing in...";

  try {
    const response = await fetch("/api/admin/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        username: values.get("username"),
        password: values.get("password")
      })
    });

    if (!response.ok) {
      const problem = await response.json().catch(() => ({}));
      throw new Error(problem.detail || "Unable to sign in.");
    }

    const result = await response.json();
    window.location.replace(result.redirectUrl || "/admin");
  } catch (error) {
    loginError.textContent = error.message;
    loginError.hidden = false;
  } finally {
    submitButton.disabled = false;
    submitButton.textContent = "Sign in";
  }
});
