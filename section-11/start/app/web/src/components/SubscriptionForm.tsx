import { useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createSubscription, type BillingCycle } from "../lib/api";
import { CURRENCY_CODES } from "../lib/currency";

const BILLING_CYCLES: BillingCycle[] = ["Monthly", "Annual"];

function today(): string {
  return new Date().toISOString().slice(0, 10);
}

export function SubscriptionForm() {
  const queryClient = useQueryClient();
  const [vendor, setVendor] = useState("");
  const [amount, setAmount] = useState("");
  const [currency, setCurrency] = useState(CURRENCY_CODES[0]);
  const [billingCycle, setBillingCycle] = useState<BillingCycle>("Monthly");
  const [renewalDate, setRenewalDate] = useState(today());
  const [error, setError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: createSubscription,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["subscriptions"] });
      setVendor("");
      setAmount("");
      setRenewalDate(today());
    },
    onError: (err: Error) => setError(err.message),
  });

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);

    if (vendor.trim().length === 0) {
      setError("Vendor is required.");
      return;
    }
    const parsedAmount = Number(amount);
    if (!Number.isFinite(parsedAmount) || parsedAmount <= 0) {
      setError("Amount must be greater than zero.");
      return;
    }

    mutation.mutate({
      vendor: vendor.trim(),
      amount: parsedAmount,
      currency,
      billingCycle,
      renewalDate,
    });
  }

  return (
    <form className="card form" onSubmit={handleSubmit}>
      <h2>Add subscription</h2>
      <label>
        Vendor
        <input
          type="text"
          maxLength={200}
          value={vendor}
          onChange={(e) => setVendor(e.target.value)}
          placeholder="GitHub, Figma, Slack..."
          required
        />
      </label>
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
      <div className="form-row">
        <label>
          Billing cycle
          <select
            value={billingCycle}
            onChange={(e) => setBillingCycle(e.target.value as BillingCycle)}
          >
            {BILLING_CYCLES.map((cycle) => (
              <option key={cycle} value={cycle}>
                {cycle}
              </option>
            ))}
          </select>
        </label>
        <label>
          Renewal date
          <input
            type="date"
            value={renewalDate}
            onChange={(e) => setRenewalDate(e.target.value)}
            required
          />
        </label>
      </div>
      {error && <p className="error">{error}</p>}
      <button type="submit" disabled={mutation.isPending}>
        {mutation.isPending ? "Saving..." : "Add subscription"}
      </button>
    </form>
  );
}
