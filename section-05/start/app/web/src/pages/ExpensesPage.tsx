import { ExpenseForm } from "../components/ExpenseForm";
import { ExpenseTable } from "../components/ExpenseTable";
import { MonthlyTotals } from "../components/MonthlyTotals";

export function ExpensesPage() {
  return (
    <div className="layout">
      <section className="main">
        <ExpenseForm />
        <div className="card">
          <h2>Expenses</h2>
          <ExpenseTable />
        </div>
      </section>
      <aside className="side">
        <div className="card">
          <h2>Monthly totals</h2>
          <MonthlyTotals />
        </div>
      </aside>
    </div>
  );
}
