import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  deleteSubscription,
  getSubscriptions,
  updateSubscriptionStatus,
  type SubscriptionStatus,
} from "../lib/api";
import { formatAmount } from "../lib/currency";

const STATUSES: SubscriptionStatus[] = ["Active", "Cancelled"];

type StatusFilter = SubscriptionStatus | "All";

export function SubscriptionTable() {
  const queryClient = useQueryClient();
  const [filter, setFilter] = useState<StatusFilter>("All");

  const {
    data: subscriptions,
    isLoading,
    isError,
  } = useQuery({
    queryKey: ["subscriptions", filter],
    queryFn: () => getSubscriptions(filter === "All" ? undefined : filter),
  });

  const remove = useMutation({
    mutationFn: deleteSubscription,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["subscriptions"] });
    },
  });

  const changeStatus = useMutation({
    mutationFn: ({ id, status }: { id: string; status: SubscriptionStatus }) =>
      updateSubscriptionStatus(id, { status }),
    onSuccess: () => {
      // Invalidating the bare key matches every filtered variant, so a row that no
      // longer matches the active filter leaves the list instead of lingering.
      queryClient.invalidateQueries({ queryKey: ["subscriptions"] });
    },
  });

  const isChanging = (id: string) => changeStatus.isPending && changeStatus.variables?.id === id;

  // The filter control renders outside the early returns. Filtering down to an empty
  // list must not take away the only way to clear the filter.
  const filterControl = (
    <div className="form-row">
      <label>
        Filter by status
        <select
          aria-label="Filter by status"
          value={filter}
          onChange={(event) => setFilter(event.target.value as StatusFilter)}
        >
          <option value="All">All</option>
          {STATUSES.map((status) => (
            <option key={status} value={status}>
              {status}
            </option>
          ))}
        </select>
      </label>
    </div>
  );

  function body() {
    if (isLoading) {
      return <p>Loading subscriptions...</p>;
    }
    if (isError) {
      return <p className="error">Could not load subscriptions. Is the API running?</p>;
    }
    if (!subscriptions || subscriptions.length === 0) {
      return filter === "All" ? (
        <p className="muted">No subscriptions yet. Add one above.</p>
      ) : (
        <p className="muted">No {filter.toLowerCase()} subscriptions.</p>
      );
    }

    return (
      <table className="table">
        <thead>
          <tr>
            <th>Renewal</th>
            <th>Vendor</th>
            <th>Cycle</th>
            <th>Status</th>
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
              <td>
                <select
                  aria-label={`Status for ${subscription.vendor}`}
                  value={subscription.status}
                  disabled={isChanging(subscription.id)}
                  onChange={(event) =>
                    changeStatus.mutate({
                      id: subscription.id,
                      status: event.target.value as SubscriptionStatus,
                    })
                  }
                >
                  {STATUSES.map((status) => (
                    <option key={status} value={status}>
                      {status}
                    </option>
                  ))}
                </select>
              </td>
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

  return (
    <>
      {filterControl}
      {changeStatus.isError && (
        <p className="error">Could not change the status. {changeStatus.error.message}</p>
      )}
      {body()}
    </>
  );
}
