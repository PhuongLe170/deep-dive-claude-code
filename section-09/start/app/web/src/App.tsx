import { NavLink, Route, Routes } from "react-router-dom";
import { ExpensesPage } from "./pages/ExpensesPage";
import { SubscriptionsPage } from "./pages/SubscriptionsPage";

export default function App() {
  return (
    <div className="app">
      <header className="app-header">
        <h1>Expense Tracker</h1>
        <nav>
          <NavLink to="/" end>
            Expenses
          </NavLink>
          <NavLink to="/subscriptions">Subscriptions</NavLink>
        </nav>
      </header>
      <main className="app-main">
        <Routes>
          <Route path="/" element={<ExpensesPage />} />
          <Route path="/subscriptions" element={<SubscriptionsPage />} />
        </Routes>
      </main>
    </div>
  );
}
