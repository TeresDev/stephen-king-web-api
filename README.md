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
| GET | `/scalar/v1` | Browsable API reference UI (Scalar) |

### Query parameters

`page` (>= 1, default 1) · `pageSize` (1-100, default 20) · `sort` · `dir` (`asc`/`desc`) ·
`year` (1900-2100, `/villains/maine` only) · `top` (1-50, summary only)

Sort keys: `/villains/maine` accepts `pages` (default, desc), `bookName`, `villainName`;
`/villains/female-derry` accepts `villainName` only.

**Paging is measured in rows, not year groups.** Year grouping is applied to each page for
presentation, so a year whose rows straddle a boundary appears on both pages.

### Ordering: why the flat sequence is not strictly pages-descending

Rows are sorted by page count descending, then grouped by publish year. **Within every group the
order is strictly descending.** Across groups it is not, and that is intentional.

A group is positioned by its longest book and carries its shorter books with it. With the live
data, 1992 holds books of 352 and 305 pages, so the sequence runs:

```
… 352, 352, 352, 305, 319, 199 …
                  ↑     ↑
        1992's short book sits ahead of 1981's longer one
```

This is inherent to grouping, not a defect. No ordering can simultaneously keep every year's rows
contiguous *and* read pages-descending from top to bottom, unless no two years interleave by page
count — and this data interleaves.

The alternatives were considered and rejected: ordering groups by year abandons page ordering
between groups entirely, and emitting one group per contiguous run makes the flat sequence
strictly descending but lets the same year appear more than once in a page. The current behaviour
keeps one group per year and orders groups by their longest book.

**Please do not "fix" this** — it is the chosen reading of "grouped by publishYear and ordered by
pages (descending)": items are ordered descending within each group.

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

### The envelope's diagnostic arrays

- **`skipped[]`** — records that could not be assembled into a response row even though the
  request otherwise succeeded. Populated today. Each entry has a `reason` such as
  `NoLinkedBooks`, `InvalidIsbn`, or `UnresolvedBookReference` — see `SkipReason` for the full
  list.
- **`errors[]`** — upstream source failures that did not prevent the request from succeeding.
  Reserved, and currently always empty **by design, not by oversight**: any upstream failure in
  the present design prevents the endpoint from serving its data at all, so it is reported as a
  `ProblemDetails` response (see the table above) instead of a partial 200 carrying `errors[]`.
  The field is kept for the day a partial-failure mode exists — a third upstream source,
  per-resource fan-out, or serving stale-with-warning.

## Running

```bash
dotnet run --project src/Investec.SK.Api
```

Open `/scalar/v1` in a browser for a clickable API reference (generated from the OpenAPI
document at `/openapi/v1.json`).

## Testing

```bash
dotnet test Investec.SK.sln
```

Tests never reach the network — they use golden fixtures captured from the live feed under
`tests/fixtures/` plus a stubbed `HttpMessageHandler`.

## Design notes

The design decisions that affect how this API behaves are documented here in this README and in
XML doc comments on the types that implement them — the ordering rule above, the diagnostic
arrays, and the failure-behaviour table. Working planning material is kept local and is not part
of the repository.
