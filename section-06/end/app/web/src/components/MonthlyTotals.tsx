import { useQuery } from "@tanstack/react-query";
import { getMonthlyTotals } from "../lib/api";
import { formatAmount } from "../lib/currency";

const MONTHS = [
  "January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December",
];

export function MonthlyTotals() {
  const { data: totals, isLoading, isError } = useQuery({
    queryKey: ["monthly-totals"],
    queryFn: getMonthlyTotals,
  });

  if (isLoading) {
    return <p>Loading totals...</p>;
  }
  if (isError) {
    return <p className="error">Could not load totals.</p>;
  }
  if (!totals || totals.length === 0) {
    return <p className="muted">No totals yet.</p>;
  }

  return (
    <ul className="totals">
      {totals.map((total) => (
        <li key={`${total.year}-${total.month}-${total.currency}`}>
          <span>
            {MONTHS[total.month - 1]} {total.year}
          </span>
          <strong>{formatAmount(total.total, total.currency)}</strong>
        </li>
      ))}
    </ul>
  );
}
