## QueryFingerprintValidationResult

The QueryFingerprintValidationResult class represents the validation result of a query fingerprint. It contains information about the query fingerprint's validity, validation errors, and metadata used for fingerprinting.

The QueryFingerprintValidationResult class contains the following public members:
* CommandTextHash: a string representing the command text hash
* NormalizedSql: a string representing the normalized SQL
* CallSite: a string representing the call site
* IsValid: a boolean indicating whether the query fingerprint is valid
* ValidationErrors: a string array containing validation errors

Example usage:
```csharp
var result = new QueryFingerprintValidationResult
{
    CommandTextHash = "abc123",
    NormalizedSql = "SELECT * FROM Users WHERE Id = @__userId_0",
    CallSite = "MyApp.Services.UserService.GetUser(Int32 id)",
    IsValid = true,
    ValidationErrors = Array.Empty<string>()
};
```
