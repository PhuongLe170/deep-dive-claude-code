import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { deleteSubscription, getSubscriptions } from "../lib/api";
import { formatAmount } from "../lib/currency";

export function SubscriptionTable() {
  const queryClient = useQueryClient();
  const { data: subscriptions, isLoading, isError } = useQuery({
    queryKey: ["subscriptions"],
    queryFn: getSubscriptions,
  });

  const remove = useMutation({
    mutationFn: deleteSubscription,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["subscriptions"] });
    },
  });

  if (isLoading) {
    return <p>Loading subscriptions...</p>;
  }
  if (isError) {
    return <p className="error">Could not load subscriptions. Is the API running?</p>;
  }
  if (!subscriptions || subscriptions.length === 0) {
    return <p className="muted">No subscriptions yet. Add one above.</p>;
  }

  return (
    <table className="table">
      <thead>
        <tr>
          <th>Renewal</th>
          <th>Vendor</th>
          <th>Cycle</th>
          <th className="right">Amount</th>
          <th />
        </tr>
      </thead>
      <tbody>
        {subscriptions.map((subscription) => (
          <tr key={subscription.id}>
            <td>{subscription.renewalDate}</td>
            <td>{subscription.vendor}</td>
            <td>{subscription.billingCycle}</td>
            <td className="right">{formatAmount(subscription.amount, subscription.currency)}</td>
            <td className="right">
              <button
                className="link-button"
                onClick={() => remove.mutate(subscription.id)}
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
