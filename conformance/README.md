# Selectable Unity tests

Test-only bundle selected independently of production wrapper/GUI source. Builders require explicit product roots. Core bootstrap authenticates the frozen selection before invoking the trusted product runner. A selection ID alone is not authorization. Canonical fixtures, readiness, projections and receipts remain trusted product responsibilities. No physical transport access. Selected code is not a sandbox; run only in a restricted job without publication credentials.

The initial shared portable test sources were copied from Unity_WSS_Integration_Lib commit 234db93 (Conformance~). GUI adapters were copied from GUI_Unity_WSS_Application commit abf7a94 (tools/UnityConformance). Builders were changed only to take explicit product roots and read the GUI test shim from this bundle. Canonical assertions are preserved and validated by trusted runners. The selected checkout SHA identifies every resulting bundle byte.

The selected complete suite is additive to mandatory fixed-product canonical and BLE lifecycle tests. Platform/native selector probes always execute from fixed product sources, never this selected bundle.
