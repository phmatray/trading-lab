# Cache-control spike — deferred to integration

**Date:** 2026-05-18

**Plan phase skipped:** Phase 0 Task 0.3 (`Cache-control round-trip spike`).

**Reason:** The spike requires hitting the real Anthropic API with the user's
key to empirically determine which of three paths attaches `cache_control` on
the wire when calling through `Microsoft.Extensions.AI`'s `IChatClient`. The
test is small ($0.001) but introduces an outbound API call that wasn't
explicitly authorised.

**Chosen path for `CacheControlChatClient` (Phase 5 Task 5.5):** Path B —
attach a typed `Anthropic.SDK.Messaging.CacheControl` instance to the
`TextContent.AdditionalProperties` under key `"anthropic.cache_control"`.
The SDK XML docs (`~/.nuget/packages/anthropic.sdk/5.10.0/lib/net10.0/Anthropic.SDK.xml`)
expose `CacheControl` as a typed property on native message content, which
strongly suggests the M.E.AI bridge looks for this exact shape in
`AdditionalProperties`. If runtime testing later shows Path B is wrong,
swap the body of `CacheControlChatClient.GetResponseAsync` per Path A
(string-value object) or Path C (drop down to raw `Anthropic.SDK`).

**Implication for production:** if Path B silently does nothing, there is
**no cache savings**, but there is **no functional regression** — calls
still succeed; we just pay full uncached input on every call. This matches
the current state (pre-Phase-3), so the worst case is "we shipped the
machinery but didn't save money."

**Verification path:** when the user next exercises a multi-instrument
day in production, check `response.Usage.AdditionalCounts` for
`CacheCreationInputTokens > 0` (first call) and `CacheReadInputTokens > 0`
(subsequent calls within the 5-minute TTL).
