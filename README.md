# Stephen King Web API

A .NET 10 minimal API over the public Stephen King Books and Villains feeds.

## Endpoints

| Method | Route | Description |
|---|---|---|
| GET | `/villains/maine` | Maine-resident villains joined to their books, grouped by publish year |
| GET | `/villains/female-derry` | Female villains resident in Derry (tolerates the "Dery" spelling) |
| GET | `/villains/maine/summary` | Row counts per year and the longest books |
| GET | `/health/upstream` | Per-source upstream reachability |
| GET | `/openapi/v1.json` | OpenAPI document |

### Query parameters

`page` (>= 1, default 1) · `pageSize` (1-100, default 20) · `sort` · `dir` (`asc`/`desc`) ·
`year` (1900-2100, `/villains/maine` only) · `top` (1-50, summary only)

Sort keys: `/villains/maine` accepts `pages` (default, desc), `bookName`, `villainName`;
`/villains/female-derry` accepts `villainName` only.

**Paging is measured in rows, not year groups.** Year grouping is applied to each page for
presentation, so a year whose rows straddle a boundary appears on both pages.

## Failure behaviour

| Situation | `/villains/maine` | `/villains/female-derry` |
|---|---|---|
| Both sources healthy | 200 | 200 |
| Books down | 502 | **200, full result** |
| Villains down | 502 | 502 |
| Both down | 503 + `Retry-After` | 502 |
| Invalid query | 400 | 400 |
| Page beyond last | 200, empty `data` | 200, empty `data` |

`/villains/female-derry` depends only on the villains source (see above) and never calls the
books repository, so it cannot distinguish "villains down" from "everything down" without
destroying that independence — a villains-source failure is always a single-source 502 here.

Every 200 carries `errors[]` (upstream sources that failed) and `skipped[]` (records that could
not be assembled), so nothing disappears silently. Stale data is never served: an expired cache
plus a down upstream returns 502/503 rather than last-known-good.

## Running

```bash
dotnet run --project src/Investec.SK.Api
```

## Testing

```bash
dotnet test Investec.SK.sln
```

Tests never reach the network — they use golden fixtures captured from the live feed under
`tests/fixtures/` plus a stubbed `HttpMessageHandler`.

## Design documents

- Design: `docs/superpowers/specs/2026-09-29-stephen-king-webapi-design.md`
- Plan: `docs/superpowers/plans/2026-09-29-stephen-king-webapi.md`
