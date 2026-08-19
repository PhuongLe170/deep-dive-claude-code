# Subscriptions

The solo track. Each section of the course, you build the Subscriptions equivalent of what the instructor builds on Expenses.

It lives in three places that never overlap the Expenses files edited on screen, so your work and the instructor's stay mergeable:

- `app/api/Api/Features/Subscriptions` (this folder)
- `app/tests/Api.Tests/Subscriptions`
- `app/web/src/pages/SubscriptionsPage.tsx` and its components

Keep it that way. The moment Subscriptions code lands in an Expenses file, syncing with a later section stops being conflict free.

The exercise guides are in `docs/exercises` in the course repository.
