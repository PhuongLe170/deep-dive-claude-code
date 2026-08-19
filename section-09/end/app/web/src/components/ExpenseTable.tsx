import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { deleteExpense, getExpenses } from "../lib/api";
import { formatAmount } from "../lib/currency";
import { ExpenseRowEditor } from "./ExpenseRowEditor";

export function ExpenseTable() {
  const queryClient = useQueryClient();
  const [editingId, setEditingId] = useState<string | null>(null);
  const {
    data: expenses,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["expenses"],
    queryFn: getExpenses,
  });

  const remove = useMutation({
    mutationFn: deleteExpense,
    onSuccess: (_result, deletedId) => {
      queryClient.invalidateQueries({ queryKey: ["expenses"] });
      queryClient.invalidateQueries({ queryKey: ["monthly-totals"] });
      queryClient.invalidateQueries({ queryKey: ["budget-comparison"] });
      // Close the editor only when the deleted row is the one open, so deleting a
      // different row does not discard edits in progress.
      setEditingId((current) => (current === deletedId ? null : current));
    },
  });

  // Scope the pending flag to the row actually being deleted, so one in-flight
  // delete does not disable the buttons on every other row.
  const isDeleting = (id: string) => remove.isPending && remove.variables === id;

  if (isLoading) {
    return <p>Loading expenses...</p>;
  }
  if (isError) {
    return <p className="error">Could not load expenses. Is the API running?</p>;
  }
  if (!expenses || expenses.length === 0) {
    return <p className="muted">No expenses yet. Add one above.</p>;
  }

  // The edited row can vanish under us (deleted in another tab, or the save came
  // back 404). Ignore a dangling id rather than leaving every Edit button disabled
  // with no editor on screen to cancel.
  const activeEditingId = expenses.some((e) => e.id === editingId) ? editingId : null;

  return (
    <table className="table">
      <thead>
        <tr>
          <th>Date</th>
          <th>Description</th>
          <th className="right">Amount</th>
          <th />
        </tr>
      </thead>
      <tbody>
        {expenses.map((expense) =>
          expense.id === activeEditingId ? (
            <ExpenseRowEditor
              key={expense.id}
              expense={expense}
              onCancel={() => setEditingId(null)}
              onSaved={() => setEditingId(null)}
            />
          ) : (
            <tr key={expense.id}>
              <td>{expense.date}</td>
              <td>
                {expense.description}
                {expense.note && <div className="note">{expense.note}</div>}
              </td>
              <td className="right">{formatAmount(expense.amount, expense.currency)}</td>
              <td className="right">
                <div className="row-actions">
                  <button
                    className="link-button secondary"
                    onClick={() => setEditingId(expense.id)}
                    // Only one row edits at a time: opening a second would silently
                    // discard the first row's unsaved changes.
                    disabled={isDeleting(expense.id) || activeEditingId !== null}
                    title={activeEditingId !== null ? "Finish the open edit first" : undefined}
                  >
                    Edit
                  </button>
                  <button
                    className="link-button"
                    onClick={() => remove.mutate(expense.id)}
                    disabled={isDeleting(expense.id)}
                  >
                    Delete
                  </button>
                </div>
              </td>
            </tr>
          ),
        )}
      </tbody>
    </table>
  );
}
