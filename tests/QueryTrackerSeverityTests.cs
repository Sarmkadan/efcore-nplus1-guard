using System;
using System.Threading;
using Xunit;
using EfCoreNPlusOneGuard;

namespace EfCoreNPlusOneGuard.Tests
{
    public class QueryTrackerSeverityTests
    {
        [Fact]
        public void Record_BelowThreshold_ReturnsNull()
        {
            var options = new NPlusOneGuardOptions { Threshold = 3 };
            var tracker = new QueryTracker(options);
            var fp = QueryFingerprint.Create("SELECT * FROM Products", "Test");

            Assert.Null(tracker.Record(fp, options));
            Assert.Null(tracker.Record(fp, options));
        }

        [Fact]
        public void Record_AtThreshold_ReturnsIncidentWithFingerprintSqlQuery()
        {
            var options = new NPlusOneGuardOptions { Threshold = 2 };
            var tracker = new QueryTracker(options);
            var fp = QueryFingerprint.Create("SELECT * FROM Products WHERE Id = 42", "Test");

            tracker.Record(fp, options);
            var incident = tracker.Record(fp, options);

            Assert.NotNull(incident);
            Assert.Equal(fp.NormalizedSql, incident.SqlQuery);
        }

        [Fact]
        public void Record_SeverityChangesAtConfiguredBoundaries()
        {
            var options = new NPlusOneGuardOptions
            {
                Threshold = 1,
                LowSeverityThreshold = 3,
                MediumSeverityThreshold = 5
            };
            var tracker = new QueryTracker(options);
            var fp = QueryFingerprint.Create("SELECT * FROM Products", "Test");

            Assert.Equal(NPlusOneSeverity.Low, tracker.Record(fp, options)!.Severity);
            Assert.Equal(NPlusOneSeverity.Low, tracker.Record(fp, options)!.Severity);
            Assert.Equal(NPlusOneSeverity.Medium, tracker.Record(fp, options)!.Severity);
            Assert.Equal(NPlusOneSeverity.Medium, tracker.Record(fp, options)!.Severity);
            Assert.Equal(NPlusOneSeverity.High, tracker.Record(fp, options)!.Severity);
        }

        [Fact]
        public void Record_WhitelistedFingerprint_SuppressesIncident()
        {
            var whitelist = new CallSiteWhitelist();
            var options = new NPlusOneGuardOptions
            {
                Threshold = 1,
                CallSiteWhitelist = whitelist
            };
            var tracker = new QueryTracker(options);
            var fp = QueryFingerprint.Create("SELECT * FROM Products", "Test");
            whitelist.AddFingerprint(fp.CommandTextHash);

            var incident = tracker.Record(fp, options);

            Assert.Null(incident);
        }

        [Fact]
        public void Record_OutsideDetectionWindow_DoesNotTriggerIncident()
        {
            var options = new NPlusOneGuardOptions
            {
                Threshold = 2,
                DetectionWindow = TimeSpan.FromMilliseconds(50)
            };
            var tracker = new QueryTracker(options);
            var fp = QueryFingerprint.Create("SELECT * FROM Products", "Test");

            Assert.Null(tracker.Record(fp, options));
            Thread.Sleep(TimeSpan.FromMilliseconds(150));

            Assert.Null(tracker.Record(fp, options));
            Assert.NotNull(tracker.Record(fp, options));
        }
    }
}
