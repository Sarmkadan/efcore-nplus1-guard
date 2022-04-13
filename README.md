# efcore-nplus1-guard

Detects EF Core N+1 query patterns at runtime with one line in AddDbContext.

## CallSiteWhitelist

The `CallSiteWhitelist` class allows you to specify which call sites are allowed to execute. You can add specific call sites using the `Add` method, or add patterns using the `AddPattern` method. For example:

