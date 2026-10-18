# GUI Unity propagation — prepared, gated

This repository targets **dev**, matching the current C# wrapper/CLI cascade.
GitHub-hosted CI is allowed; Codex development remains local Windows. The user uses
Unity Personal only. No supported unattended hosted Personal activation route was
verified, so the prepared workflows fail explicitly rather than accept a skipped
Unity test. No local runner or manual-evidence handoff has been selected or added.

Prepared paths:

- `workflows/unity-ci.yml`: committed gitlink/dependency acceptance; stable PR check
  **CLI general conformance**.
- `workflows/wss-preflight.yml`: exact Core candidate plus immutable wrapper/GUI
  source acceptance; stable evidence check **CLI general conformance**.
- `workflows/promote-integration.yml`: validate both Unity preflights, actual reviewed
  wrapper merge and normal wrapper CI, then open/reuse a gitlink-only dev PR. No merge.
- `unity-ci.json`: pinned2022.3.62f3/UTF1.1.33/fixture/case contract; explicit
  unapproved baseline and pending hosted-license state.
- `../tools/UnityConformance/Prepare-Harness.ps1`: retains the passing fixture;
  accepts an explicit wrapper root and a fresh candidate-only projection so
  committed stale DLLs cannot enter candidate validation.

Shared definitions and scripts belong to **Unity_WSS_Integration_Lib**, under
`.github/workflows/_unity-validation.yml` and `.github/scripts/`. The GUI uses trusted
wrapper dev tools, not code from a caller-nominated merge SHA. Source and tool
checkouts are siblings so clean-source validation stays meaningful. Publication
tokens are target-only and minted after preparation/provenance validation.

GUI dev pins the wrapper dev baseline after that exact commit is verified remotely
fetchable. The wrapper preserves the newer local history, including the previously
unavailable `ce6d9ef00fc2512540366b0bbe33217654b14566`. Review these dev baselines,
make workflow definitions available on default branches through the user's manual
main merges, and pin an accepted GUI harness SHA before enabling automation. Do not
substitute older wrapper main. Baseline approval flags remain disabled.

Display names intentionally mirror the C# counterparts: **CLI CI**, **WSS CLI
preflight**, and **Promote Integration submodule**. Both normal and candidate
acceptance emit **CLI general conformance**; validators distinguish them by exact
workflow path, event and source SHA. The Unity-specific filenames/schema roles do
not change.

The same ten existing cases and eighteen assertion expressions run through the
application conformance entry point and Unity wrapper. Actual local EditMode already
passed all ten; hosted automation remains unexecuted. UI managers, player packaging,
hardware and shipping releases are outside that guarantee. No new app suite or
PlayMode CI was added.

See the wrapper `.github/UNITY_AUTOMATION.md` for schema, source/resolver reuse,
names-only configuration inventory, official Personal-license references, actual
local static verification and the separately proposed WSSCore handoff. Do not add
ruleset requirements expecting green Unity checks until the hosted execution and
baseline blockers are resolved and the check names are proven by real runs.
