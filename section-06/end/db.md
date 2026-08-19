

  ┌───────────────────────────────────────────────────────────────────┐
  │                            Expenses                               │
  ├──────────────┬────────────────────────┬───────────────────────────┤
  │ PK  Id       │ uuid                   │ NOT NULL, client-gen Guid │
  │     Amount   │ numeric(18,2)          │ NOT NULL                  │
  │     Currency │ varchar(3)             │ NOT NULL                  │
  │     Descrip. │ varchar(200)           │ NOT NULL                  │
  │     Date     │ date                   │ NOT NULL                  │
  │     CreatedAt│ timestamptz            │ NOT NULL, DEFAULT now()   │
  └──────────────┴────────────────────────┴───────────────────────────┘

          (no foreign keys, no join tables, no indexes beyond PK)


          ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─
         ╎  Subscription  (C# class only — NOT in the DB)  ╎
         ╎  Id, Vendor, Amount, Currency,                  ╎
         ╎  BillingCycle (enum), RenewalDate               ╎
         ╎  Not on DbContext. No table. No migration.      ╎
          ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─

