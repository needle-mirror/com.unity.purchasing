# IAP SDK Event Schemas

Reference-only mirror of the proto schema describing the events emitted by the
Unity IAP SDK. The C# code under
`Runtime/Stores/Data/Insights/PurchaseEventProtobufWriter.cs` hand-writes the
canonical proto3 binary wire format that matches this schema.

## Source

Upstream:
`Unity-Technologies/unityapis` → `adsapis/insights/producers/iapsdk/v1alpha1/iap_sdk_event.proto`
and its transitive imports.

Also mirrored, even though it's outside that import graph:
`adsapis/insights/common/v1alpha1/producer_event_type_enum.proto` — defines the
`EventType` enum we pass to the Insights Module's `LogEvent()` transport (see
`PurchaseEvent.ForwardModule`).

Files mirror the upstream paths so that `import "insights/..."` statements
resolve correctly if anyone runs `protoc -I Schemas~` against this tree.

## Why `Schemas~`?

The trailing `~` tells Unity's asset importer to skip the folder — no `.meta`
files, no AssetDatabase entries, no TextAsset clutter for end users. These
files are reference material for developers and tooling only.

## Sync instructions

When updating, copy the `.proto` files from upstream verbatim into the matching
paths under `insights/`. Do **not** include `google/protobuf/timestamp.proto`
(it's a well-known proto that ships with `protoc`) or any proto not in the
import graph rooted at `iap_sdk_event.proto` — except for the transport-layer
`producer_event_type_enum.proto` listed above.

After updating the protos, also update the matching C# mirrors under
`Runtime/Stores/Data/Insights/Models/` and the field-number table in
`Runtime/Stores/Data/Insights/PurchaseEventProtobufWriter.cs`.
