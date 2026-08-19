import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { deleteExpense, getExpenses } from "../lib/api";
import { formatAmount } from "../lib/currency";

export function ExpenseTable() {
  const queryClient = useQueryClient();
  const { data: expenses, isLoading, isError } = useQuery({
    queryKey: ["expenses"],
    queryFn: getExpenses,
  });

  const remove = useMutation({
    mutationFn: deleteExpense,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["expenses"] });
      queryClient.invalidateQueries({ queryKey: ["monthly-totals"] });
    },
  });

  if (isLoading) {
    return <p>Loading expenses...</p>;
  }
  if (isError) {
    return <p className="error">Could not load expenses. Is the API running?</p>;
  }
  if (!expenses || expenses.length === 0) {
    return <p className="muted">No expenses yet. Add one above.</p>;
  }

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
        {expenses.map((expense) => (
          <tr key={expense.id}>
            <td>{expense.date}</td>
            <td>{expense.description}</td>
            <td className="right">{formatAmount(expense.amount, expense.currency)}</td>
            <td className="right">
              <button
                className="link-button"
                onClick={() => remove.mutate(expense.id)}
                disabled={remove.isPending}
              >
                Delete
              </button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
