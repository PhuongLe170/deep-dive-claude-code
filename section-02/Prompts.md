# Claude Code Deep Dive - Section 2: How Claude Code Works Prompts

These are the prompts run on screen during this section's lessons. Copy them as they are into Claude Code and follow along.

## L3 - Watching It Work

Traces how a single expense flows from the React form to Postgres and back into the list, without changing any code.

```
Explore this codebase and trace how a single expense flows from the React form all the way to Postgres and back into the list. 
Walk the real code path: which component captures the form input and sends the request, which endpoint receives it, how it's validated, how it's mapped and persisted through EF Core, and how the list re-renders after a successful create. 
Read the actual files, cite each one by path, and name the key functions or types at every hop. 
Don't change anything, just map it and tell me where the request crosses from web to api to the database.
```

## L4 - Choosing Model and Effort

Adds pagination to `GET /api/expenses`, choosing a paging strategy, shaping the response, and covering it with an integration test.

```
Add pagination to GET /api/expenses. Support page size and positioning via query parameters, decide on and implement a sensible paging strategy, shape a response that tells the client how to get the next page, keep the ordering stable, and add an integration test covering at least two pages. Follow the conventions already in this codebase.
```