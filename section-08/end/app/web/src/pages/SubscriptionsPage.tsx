import { SubscriptionForm } from "../components/SubscriptionForm";
import { SubscriptionTable } from "../components/SubscriptionTable";

export function SubscriptionsPage() {
  return (
    <div className="layout">
      <section className="main">
        <SubscriptionForm />
        <div className="card">
          <h2>Subscriptions</h2>
          <SubscriptionTable />
        </div>
      </section>
    </div>
  );
}
