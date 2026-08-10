# Issues: hotfix-harden-ci-arm64-fork-pr-exposure

## Close ARM64 runner fork-PR exposure (push-only CI trigger)

**GitHub issue**: #409

**Blocked by**: None

**User stories**: 1, 2, 6

### What to build

Remove the `pull_request` trigger from `ci-armv7.yml`, leaving `push` only,
so a fork PR can no longer cause code execution on the self-hosted
`hueshift-runner`. Same-repo PRs keep their CI signal via SHA-matching on
the push-triggered run. Record the reasoning in ADR-0003 (already drafted
during design — confirm it's committed alongside this change).

### Acceptance criteria

- [ ] `ci-armv7.yml` no longer has a `pull_request:` trigger block
- [ ] `ci-armv7.yml` still has its `push:` trigger, unchanged (`branches: ['**']`)
- [ ] No other job content in `ci-armv7.yml` changes
- [ ] ADR-0003 is present at `.agent-docs/adr/0003-ci-arm64-push-only-trigger.md`
- [ ] A push on this branch triggers the workflow (verified by this PR's own CI run)

---

## Add CODEOWNERS and required-review branch protection ruleset

**GitHub issue**: #410

**Blocked by**: None

**User stories**: 3, 4, 5

### What to build

Add a repo-root CODEOWNERS file naming the sole maintainer, and create a new
GitHub ruleset on `master` requiring one code-owner-approved review before
merge, with an admin bypass so the solo maintainer can still merge their own
PRs. The ruleset also blocks force-push and branch deletion on `master`.
Created via `gh api` so it's reproducible, not via the web UI.

### Acceptance criteria

- [ ] `CODEOWNERS` exists at repo root with `* @mholubinka1`
- [ ] A new ruleset targeting `master` exists via `gh api repos/mholubinka1/hueshift2/rulesets`
- [ ] The ruleset's `pull_request` rule has `required_approving_review_count: 1` and `require_code_owner_review: true`
- [ ] The ruleset includes `non_fast_forward` and `deletion` rules
- [ ] The ruleset enforcement is `active`
- [ ] The ruleset has a bypass actor for the repository admin role set to `always`

---
