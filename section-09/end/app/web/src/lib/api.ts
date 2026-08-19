const API_BASE = import.meta.env.VITE_API_BASE ?? "http://localhost:5033";

export interface Expense {
  id: string;
  amount: number;
  currency: string;
  description: string;
  date: string; // ISO date (yyyy-MM-dd)
  createdAt: string;
  note: string | null;
}

export interface CreateExpenseRequest {
  amount: number;
  currency: string;
  description: string;
  date: string;
  note?: string | null;
}

export interface UpdateExpenseRequest {
  amount: number;
  currency: string;
  description: string;
  date: string;
  note?: string | null;
}

export interface MonthlyTotal {
  year: number;
  month: number;
  currency: string;
  total: number;
}

export interface MonthlyBudget {
  id: string;
  currency: string;
  amount: number;
  updatedAt: string; // ISO date-time (DateTimeOffset)
}

export interface SetBudgetRequest {
  currency: string;
  amount: number;
}

export interface BudgetComparison {
  year: number;
  month: number;
  currency: string;
  spent: number;
  budget: number | null;
  remaining: number | null;
}

// Subscriptions track. Shares this file with Expenses; keep both sets of exports.
export type BillingCycle = "Monthly" | "Annual";

// Spelled exactly as the C# SubscriptionStatus members, because the API sends enums
// as strings and a casing slip here is a silent mismatch, not a type error.
export type SubscriptionStatus = "Active" | "Cancelled";

export interface Subscription {
  id: string;
  vendor: string;
  amount: number;
  currency: string;
  billingCycle: BillingCycle;
  renewalDate: string; // ISO date (yyyy-MM-dd)
  status: SubscriptionStatus;
}

export interface CreateSubscriptionRequest {
  vendor: string;
  amount: number;
  currency: string;
  billingCycle: BillingCycle;
  renewalDate: string;
}

export interface UpdateSubscriptionStatusRequest {
  status: SubscriptionStatus;
}

/// Carries the HTTP status so callers can react to a 404, and the problem+json
/// `errors` dictionary when the API sent one.
export class ApiError extends Error {
  readonly status: number;
  readonly errors: Record<string, string[]>;

  constructor(message: string, status: number, errors: Record<string, string[]> = {}) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.errors = errors;
  }
}

async function handle<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let detail = response.statusText;
    let errors: Record<string, string[]> = {};
    try {
      const problem = await response.json();
      detail = problem.title ?? detail;
      // Binding failures return problem+json with no `errors` member at all.
      if (problem.errors && typeof problem.errors === "object") {
        errors = problem.errors;
        const messages = Object.values(errors).flat();
        if (messages.length > 0) {
          detail = messages.join(" ");
        }
      }
    } catch {
      // response had no JSON body; keep the status text
    }
    throw new ApiError(detail, response.status, errors);
  }
  return response.json() as Promise<T>;
}

export function getExpenses(): Promise<Expense[]> {
  return fetch(`${API_BASE}/api/expenses`).then(handle<Expense[]>);
}

export function getMonthlyTotals(): Promise<MonthlyTotal[]> {
  return fetch(`${API_BASE}/api/expenses/totals/monthly`).then(handle<MonthlyTotal[]>);
}

export function createExpense(body: CreateExpenseRequest): Promise<Expense> {
  return fetch(`${API_BASE}/api/expenses`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  }).then(handle<Expense>);
}

export function updateExpense(id: string, body: UpdateExpenseRequest): Promise<Expense> {
  return fetch(`${API_BASE}/api/expenses/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  }).then(handle<Expense>);
}

export async function deleteExpense(id: string): Promise<void> {
  const response = await fetch(`${API_BASE}/api/expenses/${id}`, { method: "DELETE" });
  if (!response.ok) {
    throw new Error(`Failed to delete expense (${response.status})`);
  }
}

export function getBudgets(): Promise<MonthlyBudget[]> {
  return fetch(`${API_BASE}/api/expenses/budgets`).then(handle<MonthlyBudget[]>);
}

export function getBudgetComparison(): Promise<BudgetComparison[]> {
  return fetch(`${API_BASE}/api/expenses/budgets/comparison`).then(handle<BudgetComparison[]>);
}

export function setBudget(body: SetBudgetRequest): Promise<MonthlyBudget> {
  return fetch(`${API_BASE}/api/expenses/budgets`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  }).then(handle<MonthlyBudget>);
}

export function getSubscriptions(status?: SubscriptionStatus): Promise<Subscription[]> {
  const query = status ? `?status=${encodeURIComponent(status)}` : "";
  return fetch(`${API_BASE}/api/subscriptions${query}`).then(handle<Subscription[]>);
}

export function updateSubscriptionStatus(
  id: string,
  body: UpdateSubscriptionStatusRequest,
): Promise<Subscription> {
  return fetch(`${API_BASE}/api/subscriptions/${id}/status`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  }).then(handle<Subscription>);
}

export function createSubscription(body: CreateSubscriptionRequest): Promise<Subscription> {
  return fetch(`${API_BASE}/api/subscriptions`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  }).then(handle<Subscription>);
}

export async function deleteSubscription(id: string): Promise<void> {
  const response = await fetch(`${API_BASE}/api/subscriptions/${id}`, { method: "DELETE" });
  if (!response.ok) {
    throw new Error(`Failed to delete subscription (${response.status})`);
  }
}
