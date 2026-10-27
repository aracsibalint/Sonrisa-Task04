# ADR-005: Explicit sources and first-ingestion matching
Status: Accepted matching/baseline semantics — provider selection/rights remain gated in approval register.

## Context
Three categories are approved; exact sources/meaning of thresholds still need concrete acceptance.
## Decision proposal
Conditional NASA RSS and Binance BTCUSDT rolling24h. News normalized title+summary ordinal-ignore-case substring; RSS first baseline suppressed. Market absolute inclusive percentage threshold per UTC-minute observation slot, **not crossing/cooldown**. Earthquake simulated Mw inclusive minimum. Manual events share normalized matching with distinct provenance; no replay on rule changes.
## Alternatives considered
- BBC/Coinbase: broader news/USD but research found rights issues and less explicit Coinbase window boundaries.
- Crossings/cooldowns: reduce notification volume but add per-rule state and behavior not yet approved.
- AI importance classifier/general expressions: ambiguous and out of simple MVP scope.
## Consequences
NASA coverage is narrow; USDT is not USD. One qualifying market rule can notify every minute/channel. Initial RSS baseline/evicted entries may be lost. No fallback provider or retrospective matching. Live rights/access remain unresolved.
## Approval gate
User approves matching and repeat/baseline trade-offs before BE-002; source coverage/currency before adapter contracts; operator resolves permissions/region before live BE-008 acceptance. See research; reachability is not permission.
## Revisit
Provider terms/schema/access changes, required general-news/USD scope or explicitly different repeat policy.
