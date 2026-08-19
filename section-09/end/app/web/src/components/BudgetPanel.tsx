import { useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getBudgetComparison, getBudgets, setBudget } from "../lib/api";
import { CURRENCY_CODES, formatAmount } from "../lib/currency";

const MONTHS = [
  "January",
  "February",
  "March",
  "April",
  "May",
  "June",
  "July",
  "August",
  "September",
  "October",
  "November",
  "December",
];

export function BudgetPanel() {
  const queryClient = useQueryClient();
  const [currency, setCurrency] = useState(CURRENCY_CODES[0]);
  const [amount, setAmount] = useState("");
  const [error, setError] = useState<string | null>(null);

  const budgets = useQuery({ queryKey: ["budgets"], queryFn: getBudgets });
  const comparison = useQuery({ queryKey: ["budget-comparison"], queryFn: getBudgetComparison });

  const mutation = useMutation({
    mutationFn: setBudget,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["budgets"] });
      queryClient.invalidateQueries({ queryKey: ["budget-comparison"] });
      setAmount("");
    },
    onError: (err: Error) => setError(err.message),
  });

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);

    const parsedAmount = Number(amount);
    if (!Number.isFinite(parsedAmount) || parsedAmount <= 0) {
      setError("Amount must be greater than zero.");
      return;
    }

    mutation.mutate({ currency, amount: parsedAmount });
  }

  return (
    <div className="budget-panel">
      <form className="form" onSubmit={handleSubmit}>
        <div className="form-row">
          <label>
            Currency
            <select value={currency} onChange={(e) => setCurrency(e.target.value)}>
              {CURRENCY_CODES.map((code) => (
                <option key={code} value={code}>
                  {code}
                </option>
              ))}
            </select>
          </label>
          <label>
            Monthly budget
            <input
              type="number"
              step="0.01"
              min="0"
              value={amount}
              onChange={(e) => setAmount(e.target.value)}
              placeholder="0.00"
              required
            />
          </label>
        </div>
        {error && <p className="error">{error}</p>}
        <button type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? "Saving..." : "Set budget"}
        </button>
      </form>

      {budgets.isLoading ? (
        <p>Loading budgets...</p>
      ) : budgets.isError ? (
        <p className="error">Could not load budgets.</p>
      ) : !budgets.data || budgets.data.length === 0 ? (
        <p className="muted">No budgets set yet.</p>
      ) : (
        <ul className="totals">
          {budgets.data.map((budget) => (
            <li key={budget.currency}>
              <span>{budget.currency}</span>
              <strong>{formatAmount(budget.amount, budget.currency)}</strong>
            </li>
          ))}
        </ul>
      )}

      {comparison.data && comparison.data.length > 0 && (
        <>
          <h3 className="budget-subhead">Spend vs budget</h3>
          <ul className="totals">
            {comparison.data.map((row) => {
              const over = row.remaining !== null && row.remaining < 0;
              return (
                <li key={`${row.year}-${row.month}-${row.currency}`}>
                  <span>
                    {MONTHS[row.month - 1]} {row.year} · {row.currency}
                  </span>
                  <span className="right">
                    <strong>{formatAmount(row.spent, row.currency)}</strong>
                    {row.remaining !== null && (
                      <span className={over ? "error" : "muted"}>
                        {" "}
                        {over ? "over by " : "left "}
                        {formatAmount(Math.abs(row.remaining), row.currency)}
                      </span>
                    )}
                  </span>
                </li>
              );
            })}
          </ul>
        </>
      )}
    </div>
  );
}
