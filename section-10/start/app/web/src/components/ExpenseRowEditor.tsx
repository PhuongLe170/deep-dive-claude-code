import { useState, type KeyboardEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ApiError, updateExpense, type Expense, type UpdateExpenseRequest } from "../lib/api";
import { CURRENCY_CODES } from "../lib/currency";
import { toExpenseBody, validateExpenseInput } from "../lib/expenseInput";

interface ExpenseRowEditorProps {
  expense: Expense;
  onCancel: () => void;
  onSaved: () => void;
}

export function ExpenseRowEditor({ expense, onCancel, onSaved }: ExpenseRowEditorProps) {
  const queryClient = useQueryClient();
  const [amount, setAmount] = useState(String(expense.amount));
  const [currency, setCurrency] = useState(expense.currency);
  const [description, setDescription] = useState(expense.description);
  const [note, setNote] = useState(expense.note ?? "");
  const [date, setDate] = useState(expense.date);
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateExpenseRequest }) =>
      updateExpense(id, body),
    onSuccess: () => {
      // Editing an amount, currency or date moves the totals and the budget comparison too.
      queryClient.invalidateQueries({ queryKey: ["expenses"] });
      queryClient.invalidateQueries({ queryKey: ["monthly-totals"] });
      queryClient.invalidateQueries({ queryKey: ["budget-comparison"] });
      onSaved();
    },
    onError: (err: Error) => {
      setError(err.message);
      // Someone else deleted the row. Refetch so the table stops showing a phantom.
      if (err instanceof ApiError && err.status === 404) {
        queryClient.invalidateQueries({ queryKey: ["expenses"] });
      }
    },
  });

  function handleSave() {
    setError(null);

    const input = { amount, currency, description, note, date };
    const problem = validateExpenseInput(input);
    if (problem) {
      setError(problem);
      return;
    }

    mutation.mutate({ id: expense.id, body: toExpenseBody(input) });
  }

  // The row is a <tr>, not a <form>, so Enter has to be wired up by hand to match
  // the create form's behaviour.
  function handleKeyDown(event: KeyboardEvent<HTMLTableRowElement>) {
    if (event.key === "Enter" && !mutation.isPending) {
      event.preventDefault();
      handleSave();
    } else if (event.key === "Escape") {
      event.preventDefault();
      onCancel();
    }
  }

  return (
    <tr onKeyDown={handleKeyDown}>
      <td>
        <input
          type="date"
          aria-label="Date"
          value={date}
          onChange={(e) => setDate(e.target.value)}
        />
      </td>
      <td>
        <input
          type="text"
          aria-label="Description"
          maxLength={200}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="What was it for?"
        />
        <input
          type="text"
          aria-label="Note"
          maxLength={1000}
          value={note}
          onChange={(e) => setNote(e.target.value)}
          placeholder="Note (optional)"
        />
        {error && <p className="error">{error}</p>}
      </td>
      <td className="right">
        <input
          type="number"
          aria-label="Amount"
          step="0.01"
          min="0"
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
          placeholder="0.00"
        />
        <select
          aria-label="Currency"
          value={currency}
          onChange={(e) => setCurrency(e.target.value)}
        >
          {CURRENCY_CODES.map((code) => (
            <option key={code} value={code}>
              {code}
            </option>
          ))}
        </select>
      </td>
      <td className="right">
        <div className="row-actions">
          <button
            className="link-button secondary"
            onClick={handleSave}
            disabled={mutation.isPending}
          >
            {mutation.isPending ? "Saving..." : "Save"}
          </button>
          <button className="link-button muted" onClick={onCancel} disabled={mutation.isPending}>
            Cancel
          </button>
        </div>
      </td>
    </tr>
  );
}
