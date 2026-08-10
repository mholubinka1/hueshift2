# Harden CI: close ARM64 self-hosted runner exposure, add branch protection

## Problem Statement

`hueshift2` is a public repository. Its `ci-armv7.yml` workflow triggers on
both `push` and `pull_request` against `master`, runs on the self-hosted
ARM64 runner `hueshift-runner`, and unconditionally checks out the PR head
via `actions/checkout@v2` with no `ref:` override. Because GitHub Actions
merges a fork PR's workflow file with the base repo's *current* trigger
configuration, a stranger can open a fork PR against this repo and have
their code executed on the self-hosted runner — with `DOCKERHUB_USERNAME`
and `DOCKERHUB_TOKEN` secrets in scope. This is a live, exploitable
vulnerability today, not a theoretical one (confirmed via `gh api`: the
runner is online and attached).

Separately, the repo has no CODEOWNERS file and no rulesets, so nothing
requires review before a PR merges to `master`, and there is a redundant,
now-unnecessary `auto_request_review.yml` + `reviewers.yml` pair doing the
job CODEOWNERS should do instead.

## Solution

Close the runner exposure by removing the `pull_request` trigger from
`ci-armv7.yml`, leaving `push` only. A push-triggered run on a branch already
satisfies that branch's own PR's required status checks via SHA-matching, so
same-repo PRs lose no CI coverage — only fork PRs (which should never have
been executing on this runner) lose their automatic build.

Establish baseline governance: a CODEOWNERS file naming the sole maintainer,
and a new branch-protection ruleset on `master` requiring one code-owner
approval before merge, with an admin bypass so the solo maintainer isn't
locked out of merging their own work. The ruleset also blocks force-pushes
and branch deletion on `master` as standard hygiene.

`auto_request_review.yml` and `reviewers.yml` are left in place for this PR —
CODEOWNERS-driven review requests only take effect once the ruleset is live
on `master` post-merge, so deleting them now would leave a window with no
review-request mechanism at all. They are deleted in a small follow-up once
CODEOWNERS is confirmed working on a real PR.

Automatic Copilot code review is confirmed already handled by the
account-level personal setting addressed during the `octopus-monitoring`
hardening work (PR #482) — this repo has zero rulesets, so it cannot be a
repo-level `copilot_code_review` rule here, and the account-level toggle
applies across all repos. No repo-specific action needed.

"Require approval for all outside collaborators" (Settings → Actions →
General) has no public GitHub REST API and must be set manually by the user
after this PR merges.

## User Stories

1. As the repo maintainer, I want fork PRs to stop triggering the self-hosted
   ARM64 runner, so that a stranger's PR can no longer execute arbitrary code
   with my Docker Hub credentials in scope.
2. As the repo maintainer, I want same-repo PRs to keep getting the same CI
   signal they get today, so that closing the exposure doesn't degrade my own
   workflow.
3. As the repo maintainer, I want a CODEOWNERS file and required-review
   ruleset on `master`, so that merges require review by default rather than
   relying on discipline alone.
4. As the repo maintainer, I want to still be able to merge my own PRs
   without a second reviewer, so that solo-maintainer velocity isn't blocked
   by a rule meant to guard against external contributors.
5. As the repo maintainer, I want `master` protected against force-push and
   deletion, so that CI/branch-protection history can't be silently rewritten.
6. As a future maintainer reading this repo's history, I want the reasoning
   for the push-only trigger (and the deliberate absence of a fork-PR
   fallback workflow) recorded, so I don't "fix" it by re-adding the
   vulnerable trigger.

## Implementation Decisions

- `.github/workflows/ci-armv7.yml`: remove the `pull_request:` trigger block
  entirely; keep `push:` (`branches: ['**']`) unchanged. No other job
  content changes.
- New file `CODEOWNERS` at repo root: single wildcard rule, `* @mholubinka1`.
- New GitHub ruleset on `master` (created via `gh api repos/.../rulesets`,
  not the UI, so it's reproducible):
  - `pull_request` rule: `required_approving_review_count: 1`,
    `require_code_owner_review: true`.
  - `non_fast_forward` rule (blocks force-push).
  - `deletion` rule (blocks branch deletion).
  - Enforcement: `active`.
  - Bypass actors: repository admin role, `always` bypass mode — so the
    solo maintainer can merge their own PRs without a second approver.
- `.github/workflows/auto_request_review.yml` and `.github/reviewers.yml`:
  no change in this PR. Tracked as follow-up cleanup once CODEOWNERS is
  verified working post-merge (out of scope for this spec's acceptance
  criteria — see Out of Scope).
- ADR-0003 (`.agent-docs/adr/0003-ci-arm64-push-only-trigger.md`, already
  written during the design session) records the push-only decision and the
  deliberate choice not to add a fork-PR fallback workflow, mirroring
  `octopus-monitoring`'s ADR-0013 reasoning.

## Testing Decisions

This is infrastructure/CI configuration, not application code — there is no
app-level test seam to exercise. Verification is against GitHub's actual
state via `gh api`/`gh` CLI, done as part of implementation and again during
code review:

- `ci-armv7.yml`: confirm via YAML read that `pull_request` is gone and
  `push` is intact; confirm a real `git push` on this branch still triggers
  the workflow (it will, as part of this PR's own CI run).
- Ruleset: `gh api repos/mholubinka1/hueshift2/rulesets` after creation,
  confirm the `pull_request`, `non_fast_forward`, and `deletion` rules and
  the bypass actor are present with the expected values.
- CODEOWNERS: `gh api repos/mholubinka1/hueshift2/contents/CODEOWNERS`
  confirms the file lands at the repo root once merged (GitHub only
  recognizes it there, in `.github/`, or `docs/` — root is the existing
  convention already used for `dependabot.yml`'s sibling `.github/` files,
  but CODEOWNERS itself is being placed at root per the recommended
  option chosen during design).
- No automated test suite changes — this repo's existing test suite (if any)
  is unaffected and out of scope.

## Out of Scope

- Deleting `auto_request_review.yml` / `reviewers.yml` — deferred to a
  follow-up PR after CODEOWNERS is confirmed working on a real post-merge
  PR.
- Any change to Copilot auto-review settings — already resolved
  account-wide.
- Manually setting "Require approval for all outside collaborators" — no
  public API; remains a manual step for the user after merge.
- Dependabot scheduling changes, `actions_storage` migration, Copilot
  plan/budget changes (carried over as out of scope from the original
  cross-repo spec).
- Any change to the other repos covered by the cross-repo spec
  (`hypervolt-agile`, `bromley-bin-reminder`, `music-library-search`,
  `learning-react`, `skills`) — each gets its own implementation pass.
- A fork-PR fallback CI workflow — deliberately not built; see ADR-0003.

## Further Notes

This spec implements the `hueshift2`-specific section of the "CI/CD &
Copilot Pipeline Hardening — Follow-up Spec (repos 2–7)" (2026-08-08,
updated 2026-08-10), which itself follows the pattern established by
`octopus-monitoring` PR #482. Live-audited against actual GitHub API state
on 2026-08-10 immediately before this spec was written: 0 rulesets, no
CODEOWNERS at any recognized path, `hueshift-runner` online, default branch
confirmed `master`.
