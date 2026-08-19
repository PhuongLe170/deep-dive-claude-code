# Enums over the wire

Read this only when the entity you are scaffolding has an `enum` property.

## The trap

`System.Text.Json` serialises a C# enum as a **number** by default. EF Core's
`HasConversion<string>()` changes only the database column, not the JSON on the wire.
So a created resource comes back as `"someEnum": 1`, not `"someEnum": "Annual"`, and a
TypeScript `interface` that types the field as a string union will not match.

## The decision this skill makes

**Send enums as strings**, by registering `JsonStringEnumConverter` on the API. The C#
DTO and the TS interface then genuinely match, which is what `.claude/rules/frontend.md`
asks for.

The skill does NOT edit `Program.cs`, so it must **report** this to the user as part of
the Finish block:

```csharp
// Program.cs — enable string enums over the wire
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
```

## The consequence the test must handle

`WebApplicationFactory.CreateClient()` returns a plain `HttpClient`. `PostAsJsonAsync`,
`GetFromJsonAsync`, and `ReadFromJsonAsync` use their own default `JsonSerializerOptions`,
NOT the server's. The moment the API speaks `"Annual"`, the test posts a `1` and throws
reading a string back into the enum. So the generated test must opt into the same
converter and pass it on every call:

```csharp
private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
{
    Converters = { new JsonStringEnumConverter() },
};
```

Pass `Json` to every `PostAsJsonAsync` / `GetFromJsonAsync` / `ReadFromJsonAsync`, before
the `CancellationToken`. Wire this into the generated test from the start so the slice is
green immediately, not red-then-fixed.

## Display side

On the web, type the field as a string union of the enum's members and render the label
directly. No number-to-label mapping is needed once the wire format is a string.
