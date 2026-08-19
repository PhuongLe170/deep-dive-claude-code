---
paths: app/web/**
---

# Frontend rules

- Fetch through TanStack Query. Never call raw `fetch` in a component. Call `lib/api.ts` functions from `useQuery` / `useMutation` in a page, and pass the results down as props.
- New endpoints get a typed function in `lib/api.ts` alongside its request and response interfaces. Keep the shapes in sync with the API's `record` DTOs.
- `API_BASE` comes from `VITE_API_BASE`, defaulting to `http://localhost:5033`. Do not hardcode the URL elsewhere.
- pnpm is the package manager. Never npm or yarn.
