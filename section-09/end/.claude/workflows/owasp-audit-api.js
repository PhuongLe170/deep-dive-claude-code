export const meta = {
  name: 'owasp-audit-api',
  description: 'OWASP Top 10:2021 audit of app/api, one finder agent per category with adversarial verification per finding',
  phases: [
    { title: 'Find', detail: 'one agent per OWASP category, scoped to app/api' },
    { title: 'Verify', detail: 'independent skeptical re-check of every candidate finding' },
  ],
}

const REPO_ROOT = '/Users/guilhermeferreira/GitHub/dometrain-claude-code-deep-dive/section-09/end'
const API_ROOT = REPO_ROOT + '/app/api/Api'

const CONTEXT = `App: a small expense tracker. API project root: ${API_ROOT}
Structure: Program.cs (startup/middleware pipeline), Data/ExpenseDbContext.cs (EF Core DbContext), Features/{Companies,Expenses,Subscriptions}/ each containing an entity file, a *Contracts.cs file (request/response records plus explicit ToEntity()/ToResponse() mapping, no AutoMapper), and a *Endpoints.cs file (a static class with a Map*Endpoints(this IEndpointRouteBuilder) extension that groups routes with MapGroup and keeps handlers as private static methods). Migrations/ holds EF Core migrations (mostly generated). app/api/Api/Api.csproj lists NuGet package references. ${REPO_ROOT}/Directory.Build.props sets repo-wide build settings.

Scope strictly to files under app/api (do not look at app/web or app/tests — this is a deliberately narrow first pass). Do NOT attempt to read any appsettings*.json file anywhere in the repo — that path is blocked by project policy and the tool call will simply be denied; infer configuration behavior from Program.cs and code instead (e.g. the connection string is read via builder.Configuration.GetConnectionString("Postgres"), which is a normal config-based pattern, not a hardcoded secret).`

const CATEGORIES = [
  {
    key: 'A01:2021',
    title: 'Broken Access Control',
    guidance: `Check every endpoint registered in Features/*/*Endpoints.cs and how they're mapped in Program.cs. ASP.NET Core Minimal API endpoints have zero access control unless code explicitly adds it. Look for: (a) whether ANY authentication/authorization middleware or per-endpoint auth requirement exists anywhere in Program.cs or the endpoint files (search for AddAuthentication, AddAuthorization, UseAuthentication, UseAuthorization, RequireAuthorization, [Authorize]); (b) IDOR — handlers that take an id/companyId/etc. route parameter and fetch/update/delete a row by that id with no ownership/tenant check (there may be no caller identity at all to check against — that absence is itself part of the finding, but cite the specific handler); (c) mass-assignment — contracts whose ToEntity() mapping lets a client set fields it shouldn't (IDs, foreign keys, timestamps).`,
  },
  {
    key: 'A02:2021',
    title: 'Cryptographic Failures',
    guidance: `Grep app/api for hardcoded secrets/connection strings/API keys/passwords as literals in .cs files. Check whether HTTPS is enforced — search Program.cs for app.UseHttpsRedirection() and note if it is absent. Check whether any sensitive data (financial amounts, notes, PII) is logged or exposed in responses without cause. Check for weak/no hashing anywhere credentials might be stored (there may be none in this domain — that's fine, just check and say so).`,
  },
  {
    key: 'A03:2021',
    title: 'Injection',
    guidance: `Search for raw SQL construction: FromSqlRaw, FromSqlInterpolated, ExecuteSqlRaw, ExecuteSqlInterpolated, string concatenation/interpolation building SQL, Dapper/ADO.NET raw commands, or any process/shell execution built from user input. Standard EF Core LINQ (context.Set<T>().Where(x => x.Id == id), etc.) is parameterized and is NOT injection — do not flag plain LINQ usage as a false positive. Also check for log-injection (unsanitized user input written directly into logs enabling log forging) if any logging exists, and any dynamic query/sort/filter building that splices user input straight into a query string.`,
  },
  {
    key: 'A04:2021',
    title: 'Insecure Design',
    guidance: `Look for missing business-logic safeguards baked into the design, not a single coding typo: absence of rate limiting/throttling; endpoints that return unbounded result sets with no pagination limit; lack of tenant/company data isolation in the domain model despite a Companies feature existing (can data cross between companies with no company-scoping check anywhere in the query/handler?); trust boundary issues where the client sends authoritative data (amounts, totals, dates) that should be server-computed or validated; missing validation on business invariants (negative expense amounts, invalid currency codes, budget bounds) — check *Contracts.cs and the handlers for validation logic per category "Validate in the handler" convention.`,
  },
  {
    key: 'A05:2021',
    title: 'Security Misconfiguration',
    guidance: `Read Program.cs closely end to end. Is app.UseHttpsRedirection() present? Are any security headers set (HSTS, X-Content-Type-Options, etc.)? Is the CORS policy properly scoped — check AddCors/UseCors: is it restricted to a specific dev origin only, or could AllowAnyOrigin/AllowAnyHeader/AllowAnyMethod combine unsafely anywhere? Does UseExceptionHandler/UseStatusCodePages/AddProblemDetails risk leaking stack traces or internal details to clients outside Development (check whether verbose error detail is gated to app.Environment.IsDevelopment())? Also check ${REPO_ROOT}/Directory.Build.props and app/api/Api/Api.csproj for build-time settings that weaken security posture (e.g. suppressed warning categories).`,
  },
  {
    key: 'A06:2021',
    title: 'Vulnerable and Outdated Components',
    guidance: `Inspect app/api/Api/Api.csproj package references and versions (Npgsql.EntityFrameworkCore.PostgreSQL, Microsoft.EntityFrameworkCore.Design) and the TargetFramework. Inspect ${REPO_ROOT}/Directory.Build.props for anything that weakens NuGet vulnerability auditing — in particular check for a WarningsNotAsErrors entry containing NuGet security-advisory codes NU1901 (low), NU1902 (moderate), NU1903 (high), NU1904 (critical): downgrading those from build-breaking to mere warnings means a known-vulnerable package can ship without failing CI. Cite the exact line. Also check whether TargetFramework is a currently-supported .NET version.`,
  },
  {
    key: 'A07:2021',
    title: 'Identification and Authentication Failures',
    guidance: `Determine whether the API has ANY authentication mechanism at all. Search Program.cs for AddAuthentication/AddAuthorization/UseAuthentication/UseAuthorization, and search every Features/*/*Endpoints.cs for .RequireAuthorization() or [Authorize]. If none of these exist anywhere, that absence is the finding: every endpoint is fully anonymous with no concept of caller identity anywhere in the API. Cite the specific line range in Program.cs where the middleware pipeline is built (between builder.Build() and app.Run()) to show authentication middleware is absent, plus one or two representative endpoint-mapping lines with no authorization requirement.`,
  },
  {
    key: 'A08:2021',
    title: 'Software and Data Integrity Failures',
    guidance: `Look for: unsafe/insecure deserialization (custom JSON converters that trust embedded type info, BinaryFormatter, etc. — check ConfigureHttpJsonOptions usage in Program.cs); missing integrity/concurrency controls on updates (no [ConcurrencyCheck]/RowVersion on entities, meaning concurrent writes can silently clobber each other — assess whether that's a real integrity risk for this domain, e.g. budgets/expenses/subscriptions); any Database.Migrate() call at startup that auto-applies migrations at runtime, which trusts migration files execute unsupervised. Only report with concrete file/line evidence.`,
  },
  {
    key: 'A09:2021',
    title: 'Security Logging and Monitoring Failures',
    guidance: `Search the whole app/api/Api tree for any use of ILogger, logging calls, or any logging framework. Check Program.cs for logging configuration. Check every handler in Features/*/*Endpoints.cs for logging of security-relevant events: failed validation, exceptions, deletes, budget/company changes. If you find no logging of security-relevant events anywhere in the API, that absence is the finding — cite the relevant handler file/lines where such an event occurs silently (e.g. a DELETE handler with no audit trail).`,
  },
  {
    key: 'A10:2021',
    title: 'Server-Side Request Forgery (SSRF)',
    guidance: `Search for any outbound network call the API makes based on client-supplied data: HttpClient, IHttpClientFactory, WebClient, HttpWebRequest, or any URL/host/endpoint value taken from a request body/query/route parameter and then used to make a server-side request. If the API makes no outbound HTTP calls driven by user input anywhere in app/api, this category has no findings — say so plainly rather than forcing something in.`,
  },
]

const FINDINGS_SCHEMA = {
  type: 'object',
  properties: {
    findings: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          file: { type: 'string', description: 'repo-relative path, e.g. app/api/Api/Program.cs' },
          line: { type: 'integer' },
          title: { type: 'string' },
          summary: { type: 'string' },
          evidence: { type: 'string' },
          severity: { type: 'string', enum: ['low', 'medium', 'high', 'critical'] },
        },
        required: ['file', 'line', 'title', 'summary', 'severity'],
      },
    },
  },
  required: ['findings'],
}

const VERDICT_SCHEMA = {
  type: 'object',
  properties: {
    real: { type: 'boolean' },
    reasoning: { type: 'string' },
  },
  required: ['real', 'reasoning'],
}

function findPrompt(cat) {
  return `You are conducting a security audit of a .NET 10 Minimal API codebase, scoped ONLY to OWASP Top 10:2021 category ${cat.key} — ${cat.title}.

${CONTEXT}

Category-specific guidance: ${cat.guidance}

Read the actual current source files before reporting anything — do not guess or rely on general knowledge of what a typical app has. For every finding, cite the exact repo-relative file path (e.g. "app/api/Api/Program.cs") and the exact 1-indexed line number in the file as it exists right now. Only report something you can point to a specific line for; general advice with no concrete location does not count. A missing security control (e.g. no auth middleware at all) still needs a citation — point to the file/line where the control should be but isn't.

If you find nothing genuinely in-scope for this category, return an empty findings array. Do not stretch unrelated code into a finding just to have something to report — empty is a valid and useful result.`
}

function verifyPrompt(cat, f) {
  return `You are adversarially verifying a single security finding from an OWASP Top 10 audit before it is allowed into the final report. Be skeptical — default to real:false if you cannot independently confirm it.

${CONTEXT}

Claimed finding:
- OWASP category: ${cat.key} — ${cat.title}
- File: ${f.file}
- Line: ${f.line}
- Title: ${f.title}
- Summary: ${f.summary}
- Evidence cited: ${f.evidence || '(none given)'}
- Claimed severity: ${f.severity}

Open the cited file yourself (the line may have shifted by a line or two — check the surrounding ~15 lines too) and confirm:
1. The code (or absence of code) described actually exists at or near that location.
2. The security claim is technically accurate for this .NET 10 Minimal API + EF Core + Npgsql codebase, not a misunderstanding of framework defaults (e.g. EF Core LINQ queries are parameterized by default and are NOT SQL injection; Minimal API endpoints have no auth applied by default unless middleware/attributes add it — that is a real gap if true, not a misunderstanding).
3. It genuinely maps to the stated OWASP category, not a different one.
4. The claimed severity is reasonable given actual exploitability and impact — adjust it in your reasoning if not, but this alone should not flip real to false.

Return real:true only if points 1-3 hold. Otherwise real:false with reasoning explaining exactly why it fails.`
}

const results = await pipeline(
  CATEGORIES,
  cat => agent(findPrompt(cat), { label: `find:${cat.key}`, phase: 'Find', schema: FINDINGS_SCHEMA }),
  async (findResult, cat) => {
    const candidates = (findResult && findResult.findings) || []
    log(`${cat.key} ${cat.title}: ${candidates.length} candidate finding(s)`)
    if (candidates.length === 0) {
      return { key: cat.key, title: cat.title, candidates: [], confirmed: [], rejected: [] }
    }
    const verified = await parallel(candidates.map(f => () =>
      agent(verifyPrompt(cat, f), { label: `verify:${cat.key}`, phase: 'Verify', schema: VERDICT_SCHEMA })
        .then(v => ({ finding: f, verdict: v }))
    ))
    const clean = verified.filter(Boolean)
    const confirmed = clean.filter(v => v.verdict && v.verdict.real).map(v => v.finding)
    const rejected = clean
      .filter(v => !v.verdict || !v.verdict.real)
      .map(v => ({ finding: v.finding, reasoning: v.verdict ? v.verdict.reasoning : 'verification agent failed to return a result' }))
    log(`${cat.key} ${cat.title}: ${confirmed.length} confirmed, ${rejected.length} rejected`)
    return { key: cat.key, title: cat.title, candidates, confirmed, rejected }
  }
)

const allConfirmed = results.flatMap(r => r.confirmed.map(f => ({ ...f, category: r.key, categoryTitle: r.title })))
const allRejected = results.flatMap(r => r.rejected.map(x => ({ ...x.finding, category: r.key, categoryTitle: r.title, rejectionReason: x.reasoning })))
const emptyCategories = results.filter(r => r.candidates.length === 0).map(r => `${r.key} — ${r.title}`)
const noConfirmedCategories = results.filter(r => r.candidates.length > 0 && r.confirmed.length === 0).map(r => `${r.key} — ${r.title}`)
const confirmedCategories = results.filter(r => r.confirmed.length > 0).map(r => `${r.key} — ${r.title}`)

return {
  confirmed: allConfirmed,
  rejected: allRejected,
  emptyCategories,
  noConfirmedCategories,
  confirmedCategories,
  byCategory: results,
}
