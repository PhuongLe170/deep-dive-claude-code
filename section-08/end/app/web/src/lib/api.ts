const API_BASE = import.meta.env.VITE_API_BASE ?? "http://localhost:5033";

export interface Expense {
  id: string;
  amount: number;
  currency: string;
  description: string;
  date: string; // ISO date (yyyy-MM-dd)
  createdAt: string;
}

export interface CreateExpenseRequest {
  amount: number;
  currency: string;
  description: string;
  date: string;
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

export interface Subscription {
  id: string;
  vendor: string;
  amount: number;
  currency: string;
  billingCycle: BillingCycle;
  renewalDate: string; // ISO date (yyyy-MM-dd)
}

export interface CreateSubscriptionRequest {
  vendor: string;
  amount: number;
  currency: string;
  billingCycle: BillingCycle;
  renewalDate: string;
}

async function handle<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let detail = response.statusText;
    try {
      const problem = await response.json();
      detail = problem.title ?? detail;
    } catch {
      // response had no JSON body; keep the status text
    }
    throw new Error(detail);
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

export function getSubscriptions(): Promise<Subscription[]> {
  return fetch(`${API_BASE}/api/subscriptions`).then(handle<Subscription[]>);
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
