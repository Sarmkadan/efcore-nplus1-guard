var options = new NPlusOneGuardOptions
{
    Threshold = 5,
    DetectionWindow = TimeSpan.FromSeconds(10),
    ThrowOnDetection = true,
    LogOnDetection = true,
    IgnoredQueryPatterns = new List<string> { "SELECT * FROM __EFMigrationsHistory" }
};
