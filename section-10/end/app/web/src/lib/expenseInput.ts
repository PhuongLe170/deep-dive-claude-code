import type { CreateExpenseRequest } from "./api";

/// The API's numeric(18,2) column tops out at 9999999999999999.99, which a JS
/// number cannot represent exactly. Guard at the largest safe integer instead and
/// let the server be the authority on the exact bound; its 400 now carries a
/// per-field message, so the gap between the two still reads well.
export const MAX_EXPENSE_AMOUNT = Number.MAX_SAFE_INTEGER;

/// The raw string state both the create form and the inline row editor hold.
export interface ExpenseInput {
  amount: string;
  currency: string;
  description: string;
  note: string;
  date: string;
}

/// Returns the first problem with the input, or null when it is good to send.
/// The messages mirror the API's so the two agree on wording. The row editor is a
/// bare <tr> rather than a <form>, so nothing here can rely on native HTML5
/// constraint validation; every rule has to be checked explicitly.
export function validateExpenseInput(input: ExpenseInput): string | null {
  const parsedAmount = Number(input.amount);
  if (!Number.isFinite(parsedAmount) || parsedAmount <= 0) {
    return "Amount must be greater than zero.";
  }
  if (parsedAmount > MAX_EXPENSE_AMOUNT) {
    return "Amount is too large.";
  }
  if (hasExtraDecimals(input.amount)) {
    return "Amount cannot have more than two decimal places.";
  }
  if (input.description.trim().length === 0) {
    return "Description is required.";
  }
  if (input.date.length === 0) {
    return "Date is required.";
  }
  return null;
}

/// Builds the request body. Call only after validateExpenseInput returns null.
export function toExpenseBody(input: ExpenseInput): CreateExpenseRequest {
  const trimmedNote = input.note.trim();
  return {
    amount: Number(input.amount),
    currency: input.currency,
    description: input.description.trim(),
    date: input.date,
    note: trimmedNote.length > 0 ? trimmedNote : null,
  };
}

/// The database rounds to two decimals silently, so reject the extra precision up
/// front rather than showing the user a number they did not type.
function hasExtraDecimals(amount: string): boolean {
  const fraction = amount.split(".")[1];
  return fraction !== undefined && fraction.replace(/0+$/, "").length > 2;
}
