# ARM64 CI workflow triggers on push only, not pull_request

`ci-armv7.yml` ran on `push` and `pull_request` against a self-hosted ARM64
runner (`hueshift-runner`), with an unconditional `actions/checkout@v2` and no
`ref:` override. On a public repo, this let any stranger open a fork PR and
have their code executed on the self-hosted runner. We removed the
`pull_request` trigger, keeping `push` only: a push-triggered run on a branch
already satisfies that branch's own PR's required status checks via
SHA-matching, so same-repo PRs lose no coverage.

We deliberately did **not** add a hosted-runner fallback workflow to give fork
PRs their own CI signal. `octopus-monitoring` tried exactly that (a
trusted-checkout pattern gating a GitHub-hosted job) and later removed it
(ADR-0013 there): it only ever protected GitHub-hosted infra, never the
self-hosted runner the push-only redesign already fully secures, and it
carried real complexity — two Copilot-caught trust gaps and one documented
unfixed one — for a convenience feature with no evidence of use on a
solo-maintainer project. If this repo ever gets a genuine external
contributor, add a fallback workflow then, informed by that specific case.
