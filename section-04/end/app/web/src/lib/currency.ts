// ISO 4217 code to display symbol. Mirrors the backend allowed set
// (app/api/Api/Features/Expenses/Currencies.cs). Extend both to support a new currency.
export const CURRENCIES: Record<string, string> = {
  EUR: "€",
  USD: "$",
  GBP: "£",
  JPY: "¥",
};

export const CURRENCY_CODES = Object.keys(CURRENCIES);

export function formatAmount(amount: number, currency: string): string {
  const symbol = CURRENCIES[currency] ?? "";
  return `${symbol}${amount.toFixed(2)}`;
}
