// TASK-0417 (ADR-0173): one automatic re-run of a run of the workflow "CI" whose only failures are jobs that GitHub gave no
// runner. On 2026-11-18 three of 44 runs failed only because the job on macOS ended with "The job was not acquired by Runner
// of type hosted even after multiple attempts"; no step ran, and a person had to start a re-run.
//
// The workflow .github/workflows/rerun-lost-runner.yml runs this file from the default branch. It runs no file of the
// branch of the run, because its token can start a re-run. TASK-0435 (phase P0.173): a known fault of the GitHub service in
// an upload or a download of an artifact also gets the one re-run; the script reads the names of those steps from
// .github/workflows/ci.yml of the same checkout, as text, and runs nothing of it.
//
// usage: node eng/lost-runner.js <run id>        reads the run with GITHUB_TOKEN and GITHUB_REPOSITORY, and asks for one
//                                                re-run of the failed jobs when the choice says so
//        node eng/lost-runner.js --decide <file> prints the choice for a saved run, as JSON (for the tests)
//        node eng/lost-runner.js --service-steps prints the names of the steps of an artifact in ci.yml, as JSON (for the
//                                                tests)
"use strict";
const fs = require("fs");
const path = require("path");

// The text of GitHub for a job that no runner took.
const LOST = "was not acquired by Runner";

// TASK-0435 (phase P0.173): the known faults of the GitHub service in a step of the service, each as the texts that its
// annotation holds. Run 44831092608 failed on macOS in the step "Keep the determinism hash" with "Failed to CreateArtifact:
// Failed to make request after 5 attempts: Request timeout: /twirp/github.actions.results.api.v1.ArtifactService/CreateArtifact".
// A failed set-up of an action ("Set up job") has no known text yet, so it is no step of the service.
// P0.189 (finding T202 of the codebase review of 2026-11-25): run 45707816188 failed on Ubuntu in the same step with "Failed to
// FinalizeArtifact: Unable to make request: ECONNRESET".
const SERVICE_FAULTS = [["Failed to CreateArtifact", "Request timeout"], ["Failed to FinalizeArtifact", "ECONNRESET"]];

// The names of the steps of a workflow that upload or download an artifact, from its text: each step "- name:" whose "uses:"
// is actions/upload-artifact or actions/download-artifact. The workflow of the default branch gives the names, so a step that
// a branch renamed is no step of the service, and its run stays red.
function artifactSteps(workflow) {
  const names = new Set();
  let name = null;
  for (const line of workflow.split(/\r?\n/)) {
    const step = /^\s*- name:\s*(.+?)\s*$/.exec(line);
    if (step) name = step[1];
    else if (/^\s*-\s/.test(line)) name = null;
    if (name && /^\s*uses:\s*actions\/(upload|download)-artifact@/.test(line)) names.add(name);
  }
  return names;
}

// The choice for one attempt of a run: { rerun, reason }. A re-run only when the run is "CI", it failed on its first attempt,
// and each job that did not pass failed for a fault of the GitHub service: the annotation of a lost runner and no failed step,
// or (TASK-0435) failed steps that are each an upload or a download of an artifact, with each annotation of the level
// "failure" a known fault of the service. A failed test, a crash of the test host, each other failed
// step, each other annotation of a failure and a second attempt keep the run red, so a person sees each fault of the code.
function decide({ run, jobs, annotations }, serviceSteps = new Set()) {
  if (run.name !== "CI")
    return { rerun: false, reason: `the run is of the workflow "${run.name}", not "CI"` };
  if (run.conclusion !== "failure")
    return { rerun: false, reason: `the run has the conclusion "${run.conclusion}"` };
  if (run.run_attempt !== 1)
    return { rerun: false, reason: `the run is on its attempt ${run.run_attempt}; only a first attempt gets a re-run` };
  const failed = jobs.filter(job => !["success", "skipped", "neutral"].includes(job.conclusion));
  if (failed.length === 0)
    return { rerun: false, reason: "no job failed" };
  const lostJobs = [];
  const faults = [];
  for (const job of failed) {
    const errors = (annotations[job.id] ?? []).filter(a => a.annotation_level === "failure");
    const steps = (job.steps ?? []).filter(s => s.conclusion === "failure");
    if (steps.length === 0) {
      if (!errors.some(a => a.message.includes(LOST)))
        return { rerun: false, reason: `the job "${job.name}" has no annotation "${LOST}"` };
      lostJobs.push(job.name);
      continue;
    }
    const step = steps.find(s => !serviceSteps.has(s.name));
    if (step)
      return { rerun: false, reason: `the job "${job.name}" failed in the step "${step.name}"` };
    const known = a => SERVICE_FAULTS.some(texts => texts.every(text => a.message.includes(text)));
    const other = errors.find(a => !known(a));
    if (errors.length === 0 || other)
      return { rerun: false, reason: `the job "${job.name}" failed in the step "${steps[0].name}" with no known fault of the service` + (other ? `: "${other.message.split("\n")[0]}"` : "") };
    faults.push(`"${job.name}" in the step "${steps[0].name}"`);
  }
  const parts = [];
  if (lostJobs.length > 0) parts.push(`GitHub gave no runner to ${lostJobs.map(name => `"${name}"`).join(", ")}`);
  if (faults.length > 0) parts.push(`a known fault of the GitHub service failed ${faults.join(", ")}`);
  return { rerun: true, reason: parts.join("; ") };
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

// The steps of the service in the workflow "CI" of this checkout, which the workflow of the re-run takes from the default branch.
function ciServiceSteps() {
  return artifactSteps(fs.readFileSync(path.join(__dirname, "..", ".github", "workflows", "ci.yml"), "utf8"));
}

async function main(argv) {
  if (argv[0] === "--decide") {
    console.log(JSON.stringify(decide(JSON.parse(fs.readFileSync(argv[1], "utf8")), ciServiceSteps())));
    return;
  }
  if (argv[0] === "--service-steps") {
    console.log(JSON.stringify([...ciServiceSteps()]));
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

  const choice = decide({ run, jobs, annotations }, ciServiceSteps());
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

module.exports = { decide, artifactSteps };
