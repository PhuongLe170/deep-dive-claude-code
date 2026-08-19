

  ┌───────────────────────────────────────────────────────────────────────┐
  │                               Expenses                                │
  ├──────────────────┬────────────────────────┬───────────────────────────┤
  │ PK  Id           │ uuid                   │ NOT NULL, client-gen Guid │
  │     Amount       │ numeric(18,2)          │ NOT NULL                  │
  │     Currency     │ varchar(3)             │ NOT NULL                  │
  │     Description  │ varchar(200)           │ NOT NULL                  │
  │     Note         │ varchar(1000)          │ NULL                      │
  │     Date         │ date                   │ NOT NULL                  │
  │     CreatedAt    │ timestamptz            │ NOT NULL, DEFAULT now()   │
  └──────────────────┴────────────────────────┴───────────────────────────┘

  ┌───────────────────────────────────────────────────────────────────────┐
  │                            MonthlyBudgets                             │
  ├──────────────────┬────────────────────────┬───────────────────────────┤
  │ PK  Id           │ uuid                   │ NOT NULL, client-gen Guid │
  │     Currency     │ varchar(3)             │ NOT NULL, UNIQUE index    │
  │     Amount       │ numeric(18,2)          │ NOT NULL                  │
  │     UpdatedAt    │ timestamptz            │ NOT NULL, set in code     │
  └──────────────────┴────────────────────────┴───────────────────────────┘

  ┌───────────────────────────────────────────────────────────────────────┐
  │                             Subscriptions                             │
  ├──────────────────┬────────────────────────┬───────────────────────────┤
  │ PK  Id           │ uuid                   │ NOT NULL, client-gen Guid │
  │     Vendor       │ varchar(200)           │ NOT NULL                  │
  │     Amount       │ numeric(18,2)          │ NOT NULL                  │
  │     Currency     │ varchar(3)             │ NOT NULL                  │
  │     BillingCycle │ varchar(16)            │ NOT NULL, enum as string  │
  │     RenewalDate  │ date                   │ NOT NULL                  │
  └──────────────────┴────────────────────────┴───────────────────────────┘

  ┌───────────────────────────────────────────────────────────────────────┐
  │                               Companies                               │
  ├──────────────────┬────────────────────────┬───────────────────────────┤
  │ PK  Id           │ uuid                   │ NOT NULL, client-gen Guid │
  │     Name         │ varchar(200)           │ NOT NULL                  │
  │     Website      │ varchar(200)           │ NOT NULL                  │
  │     CreatedAt    │ timestamptz            │ NOT NULL, DEFAULT now()   │
  └──────────────────┴────────────────────────┴───────────────────────────┘

          (no foreign keys, no join tables, one unique index)

