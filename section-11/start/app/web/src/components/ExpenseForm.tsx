import { useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createExpense } from "../lib/api";
import { CURRENCY_CODES } from "../lib/currency";
import { toExpenseBody, validateExpenseInput } from "../lib/expenseInput";

function today(): string {
  return new Date().toISOString().slice(0, 10);
}

export function ExpenseForm() {
  const queryClient = useQueryClient();
  const [amount, setAmount] = useState("");
  const [currency, setCurrency] = useState(CURRENCY_CODES[0]);
  const [description, setDescription] = useState("");
  const [note, setNote] = useState("");
  const [date, setDate] = useState(today());
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: createExpense,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["expenses"] });
      queryClient.invalidateQueries({ queryKey: ["monthly-totals"] });
      queryClient.invalidateQueries({ queryKey: ["budget-comparison"] });
      setAmount("");
      setDescription("");
      setNote("");
      setDate(today());
    },
    onError: (err: Error) => setError(err.message),
  });

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);

    const input = { amount, currency, description, note, date };
    const problem = validateExpenseInput(input);
    if (problem) {
      setError(problem);
      return;
    }

    mutation.mutate(toExpenseBody(input));
  }

  return (
    <form className="card form" onSubmit={handleSubmit}>
      <h2>Add expense</h2>
      <div className="form-row">
        <label>
          Amount
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
      </div>
      <label>
        Description
        <input
          type="text"
          maxLength={200}
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          placeholder="What was it for?"
          required
        />
      </label>
      <label>
        Note
        <input
          type="text"
          maxLength={1000}
          value={note}
          onChange={(e) => setNote(e.target.value)}
          placeholder="Optional"
        />
      </label>
      <label>
        Date
        <input type="date" value={date} onChange={(e) => setDate(e.target.value)} required />
      </label>
      {error && <p className="error">{error}</p>}
      <button type="submit" disabled={mutation.isPending}>
        {mutation.isPending ? "Saving..." : "Add expense"}
      </button>
    </form>
  );
}
