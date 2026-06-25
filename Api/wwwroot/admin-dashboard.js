const state = {
  page: 1,
  pageSize: 25,
  totalCount: 0,
  search: ""
};

const rows = document.querySelector("#assessment-rows");
const errorMessage = document.querySelector("#dashboard-error");
const emptyState = document.querySelector("#empty-state");
const dialog = document.querySelector("#answers-dialog");

document.querySelector("#assessment-search").addEventListener("submit", event => {
  event.preventDefault();
  state.search = new FormData(event.currentTarget).get("search").trim();
  state.page = 1;
  loadAssessments();
});

document.querySelector("#clear-search").addEventListener("click", () => {
  document.querySelector("#search-input").value = "";
  state.search = "";
  state.page = 1;
  loadAssessments();
});

document.querySelector("#refresh-button").addEventListener("click", loadAssessments);
document.querySelector("#previous-page").addEventListener("click", () => {
  if (state.page > 1) {
    state.page -= 1;
    loadAssessments();
  }
});
document.querySelector("#next-page").addEventListener("click", () => {
  if (state.page * state.pageSize < state.totalCount) {
    state.page += 1;
    loadAssessments();
  }
});
document.querySelector("#close-dialog").addEventListener("click", () => dialog.close());
document.querySelector("#logout-button").addEventListener("click", logout);

async function loadAssessments() {
  setError("");
  rows.replaceChildren();
  emptyState.hidden = true;

  const query = new URLSearchParams({
    page: String(state.page),
    pageSize: String(state.pageSize)
  });
  if (state.search) query.set("search", state.search);

  try {
    const response = await authenticatedFetch(`/api/admin/assessments?${query}`);
    const result = await response.json();

    state.totalCount = result.totalCount;
    result.items.forEach(renderRow);
    emptyState.hidden = result.items.length !== 0;
    updatePagination(result);
  } catch (error) {
    setError(error.message);
  }
}

function renderRow(item) {
  const row = document.createElement("tr");
  row.append(
    cell(item.userName, "user-cell"),
    cell(formatDate(item.startedOn)),
    cell(item.completedOn ? formatDate(item.completedOn) : "—"),
    cell(formatDuration(item.totalDurationMilliseconds)),
    cell(formatScore(item.totalScore), "score-cell"),
    statusCell(item.status)
  );

  const actionCell = document.createElement("td");
  const button = document.createElement("button");
  button.type = "button";
  button.className = "view-link";
  button.textContent = "View answers";
  button.addEventListener("click", () => showAnswers(item.assessmentId, item.userName));
  actionCell.append(button);
  row.append(actionCell);
  rows.append(row);
}

async function showAnswers(assessmentId, userName) {
  document.querySelector("#dialog-title").textContent = userName;
  document.querySelector("#dialog-meta").textContent = "Loading assessment details...";
  document.querySelector("#dialog-content").replaceChildren();
  dialog.showModal();

  try {
    const response = await authenticatedFetch(
      `/api/admin/assessments/${encodeURIComponent(assessmentId)}`);
    const assessment = await response.json();
    renderAnswers(assessment);
  } catch (error) {
    document.querySelector("#dialog-meta").textContent = error.message;
  }
}

function renderAnswers(assessment) {
  const content = document.querySelector("#dialog-content");
  content.replaceChildren();
  document.querySelector("#dialog-meta").textContent =
    `${formatDate(assessment.startedOn)} · ${assessment.status} · ` +
    `Total score ${formatScore(sumScores(assessment.answers))}`;

  if (assessment.answers.length === 0) {
    content.append(element("div", "empty-state", "No selected answers were recorded."));
    return;
  }

  const groups = groupAnswers(assessment.answers);
  for (const [stepName, modules] of groups) {
    const step = element("section", "answer-step");
    step.append(element("h3", null, stepName));

    for (const [moduleName, answers] of modules) {
      const module = element("section", "answer-module");
      module.append(element("h4", null, moduleName));

      const table = element("table", "dialog-answer-table");
      const header = document.createElement("thead");
      const headerRow = document.createElement("tr");
      ["Selected question", "Answer", "Score"].forEach(label =>
        headerRow.append(element("th", null, label)));
      header.append(headerRow);

      const body = document.createElement("tbody");
      answers.forEach(answer => {
        const row = document.createElement("tr");
        row.append(
          cell(answer.question),
          cell(answer.answerText),
          cell(formatScore(answer.score), "score-cell")
        );
        body.append(row);
      });

      table.append(header, body);
      module.append(table);
      step.append(module);
    }
    content.append(step);
  }
}

async function logout() {
  await fetch("/api/admin/logout", { method: "POST" });
  window.location.replace("/admin/login");
}

async function authenticatedFetch(url) {
  const response = await fetch(url);
  if (response.status === 401 || response.status === 403) {
    window.location.replace("/admin/login");
    throw new Error("Your admin session has expired.");
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => ({}));
    throw new Error(problem.detail || problem.title || "Unable to load the report.");
  }
  return response;
}

function updatePagination(result) {
  const first = result.totalCount === 0 ? 0 : ((result.page - 1) * result.pageSize) + 1;
  const last = Math.min(result.page * result.pageSize, result.totalCount);
  document.querySelector("#total-records").textContent = result.totalCount;
  document.querySelector("#current-page").textContent = result.page;
  document.querySelector("#page-size-display").textContent = result.pageSize;
  document.querySelector("#pagination-summary").textContent =
    `Showing ${first}–${last} of ${result.totalCount}`;
  document.querySelector("#previous-page").disabled = result.page <= 1;
  document.querySelector("#next-page").disabled =
    result.page * result.pageSize >= result.totalCount;
}

function groupAnswers(answers) {
  const groups = new Map();
  answers.forEach(answer => {
    if (!groups.has(answer.stepName)) groups.set(answer.stepName, new Map());
    const modules = groups.get(answer.stepName);
    if (!modules.has(answer.moduleName)) modules.set(answer.moduleName, []);
    modules.get(answer.moduleName).push(answer);
  });
  return groups;
}

function formatDate(value) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(new Date(value));
}

function formatDuration(milliseconds) {
  if (milliseconds === null || milliseconds === undefined) return "In progress";
  const totalSeconds = Math.max(0, Math.round(milliseconds / 1000));
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return minutes ? `${minutes}m ${seconds}s` : `${seconds}s`;
}

function formatScore(value) {
  return Number(value || 0).toFixed(2).replace(/\.00$/, "");
}

function sumScores(answers) {
  return answers.reduce((sum, answer) => sum + Number(answer.score || 0), 0);
}

function cell(text, className) {
  return element("td", className, text);
}

function statusCell(status) {
  const cellNode = document.createElement("td");
  cellNode.append(element(
    "span",
    `status-badge ${status.toLowerCase()}`,
    status));
  return cellNode;
}

function element(tagName, className, text) {
  const node = document.createElement(tagName);
  if (className) node.className = className;
  if (text !== undefined) node.textContent = text;
  return node;
}

function setError(message) {
  errorMessage.textContent = message;
  errorMessage.hidden = !message;
}

loadAssessments();
