// TASK-0417 (ADR-0173): one automatic re-run of a run of the workflow "CI" whose only failures are jobs that GitHub gave no
// runner. On 2026-11-18 three of 44 runs failed only because the job on macOS ended with "The job was not acquired by Runner
// of type hosted even after multiple attempts"; no step ran, and a person had to start a re-run.
//
// The workflow .github/workflows/rerun-lost-runner.yml runs this file from the default branch. It runs no file of the
// branch of the run, because its token can start a re-run.
//
// usage: node eng/lost-runner.js <run id>        reads the run with GITHUB_TOKEN and GITHUB_REPOSITORY, and asks for one
//                                                re-run of the failed jobs when the choice says so
//        node eng/lost-runner.js --decide <file> prints the choice for a saved run, as JSON (for the tests)
"use strict";
const fs = require("fs");

// The text of GitHub for a job that no runner took.
const LOST = "was not acquired by Runner";

// The choice for one attempt of a run: { rerun, reason }. A re-run only when the run is "CI", it failed on its first attempt,
// and each job that did not pass has the annotation of a lost runner and no failed step. A failed test, a failed step or a
// second attempt keeps the run red, so a person sees each fault of the code.
function decide({ run, jobs, annotations }) {
  if (run.name !== "CI")
    return { rerun: false, reason: `the run is of the workflow "${run.name}", not "CI"` };
  if (run.conclusion !== "failure")
    return { rerun: false, reason: `the run has the conclusion "${run.conclusion}"` };
  if (run.run_attempt !== 1)
    return { rerun: false, reason: `the run is on its attempt ${run.run_attempt}; only a first attempt gets a re-run` };
  const failed = jobs.filter(job => !["success", "skipped", "neutral"].includes(job.conclusion));
  if (failed.length === 0)
    return { rerun: false, reason: "no job failed" };
  for (const job of failed) {
    const step = (job.steps ?? []).find(s => s.conclusion === "failure");
    if (step)
      return { rerun: false, reason: `the job "${job.name}" failed in the step "${step.name}"` };
    const lost = (annotations[job.id] ?? []).some(a => a.annotation_level === "failure" && a.message.includes(LOST));
    if (!lost)
      return { rerun: false, reason: `the job "${job.name}" has no annotation "${LOST}"` };
  }
  return { rerun: true, reason: `GitHub gave no runner to ${failed.map(job => `"${job.name}"`).join(", ")}` };
}

async function api(method, path) {
  const response = await fetch(`https://api.github.com/repos/${process.env.GITHUB_REPOSITORY}${path}`, {
    method,
    headers: {
      Accept: "application/vnd.github+json",
      Authorization: `Bearer ${process.env.GITHUB_TOKEN}`,
      "User-Agent": "lost-runner",
      "X-GitHub-Api-Version": "2022-11-28",
    },
  });
  if (!response.ok)
    throw new Error(`${method} ${path}: ${response.status} ${await response.text()}`);
  return response.status === 204 || response.status === 201 ? null : response.json();
}

async function main(argv) {
  if (argv[0] === "--decide") {
    console.log(JSON.stringify(decide(JSON.parse(fs.readFileSync(argv[1], "utf8")))));
    return;
  }

  const id = Number(argv[0]);
  if (!Number.isInteger(id) || id <= 0 || !process.env.GITHUB_TOKEN || !process.env.GITHUB_REPOSITORY)
    throw new Error("usage: node eng/lost-runner.js <run id>, with GITHUB_TOKEN and GITHUB_REPOSITORY");
  const run = await api("GET", `/actions/runs/${id}`);
  const jobs = (await api("GET", `/actions/runs/${id}/attempts/${run.run_attempt}/jobs?per_page=100`)).jobs;
  const annotations = {};
  for (const job of jobs) {
    if (!["success", "skipped", "neutral"].includes(job.conclusion))
      annotations[job.id] = await api("GET", `/check-runs/${job.id}/annotations`);
  }

  const choice = decide({ run, jobs, annotations });
  // The log names the run and its attempt, so a session can list a failed first attempt for the ledger (the API gives a run
  // the result of its last attempt).
  console.log(`run ${id} attempt ${run.run_attempt} (${run.head_branch} ${String(run.head_sha).slice(0, 7)}): ${choice.rerun ? "re-run" : "no re-run"}, ${choice.reason}`);
  if (choice.rerun)
    await api("POST", `/actions/runs/${id}/rerun-failed-jobs`);
}

if (require.main === module) {
  main(process.argv.slice(2)).catch(error => {
    console.error(error.message);
    process.exit(1);
  });
}

module.exports = { decide };
