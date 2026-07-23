using System;
using Xunit;
using EfCoreNPlusOneGuard;

namespace EfCoreNPlusOneGuard.Tests
{
    public class QueryTrackerTests
    {
        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new QueryTracker(null!));
        }

        [Fact]
        public void Record_BelowThreshold_ReturnsNull()
        {
            var options = new NPlusOneGuardOptions { Threshold = 3 };
            var tracker = new QueryTracker(options);
            var fp = QueryFingerprint.Create("SELECT * FROM Table", "Test");

            // Below threshold (1 < 3)
            var incident = tracker.Record(fp, options);
            Assert.Null(incident);
        }

        [Fact]
        public void Record_AboveThreshold_ReturnsIncident()
        {
            var options = new NPlusOneGuardOptions { Threshold = 2 };
            var tracker = new QueryTracker(options);
            var fp = QueryFingerprint.Create("SELECT * FROM Table", "Test");

            tracker.Record(fp, options); // Count: 1
            var incident = tracker.Record(fp, options); // Count: 2, threshold met

            Assert.NotNull(incident);
            Assert.Equal(2, incident.Count);
        }

        [Fact]
        public void Reset_ClearsRecords()
        {
            var options = new NPlusOneGuardOptions { Threshold = 2 };
            var tracker = new QueryTracker(options);
            var fp = QueryFingerprint.Create("SELECT * FROM Table", "Test");

            tracker.Record(fp, options); // Count 1
            tracker.Reset();
            
            var incident = tracker.Record(fp, options); // Still Count 1, threshold not met
            Assert.Null(incident);
        }

        [Fact]
        public void TrackExecution_ValidSql_TracksCorrectly()
        {
            var options = new NPlusOneGuardOptions { Threshold = 2, CaptureCallSite = false };
            var tracker = new QueryTracker(options);
            string sql = "SELECT * FROM Table";

            tracker.TrackExecution(sql);
            var incident = tracker.Record(QueryFingerprint.Create(sql, "QueryTracker.TrackExecution"), options);
            
            // Should be count 2 now
            Assert.NotNull(incident);
            Assert.Equal(2, incident.Count);
        }

        [Fact]
        public void TrackExecution_IgnoredSql_DoesNotTrack()
        {
            var options = new NPlusOneGuardOptions { Threshold = 1 };
            var tracker = new QueryTracker(options);
            string sql = "SELECT * FROM Table -- nplus1:ignore";

            tracker.TrackExecution(sql);
            
            // If ignored, shouldn't record anything. 
            // If it had recorded, Count would be 1, which >= Threshold 1, thus returning incident.
            // But we need to check if it did record.
            // Actually, we can check by trying to record and see if it's 2 or 1.
            
            var incident = tracker.Record(QueryFingerprint.Create(sql, "QueryTracker.TrackExecution"), options);
            
            // If it was ignored, count is 0, so next record is 1. If threshold is 1, it should return incident.
            // Wait, this is tricky. If ignored, count remains 0. If I call Record again, count becomes 1. 
            // If threshold is 1, Record returns incident.
            
            // Let's test this:
            // 1. TrackExecution ignored
            // 2. Assert that Record returns an incident (meaning it wasn't tracked previously)
            
            Assert.NotNull(incident); 
            Assert.Equal(1, incident.Count);
        }

        [Fact]
        public void TrackExecution_WithNullSql_ThrowsArgumentNullException()
        {
            var options = new NPlusOneGuardOptions();
            var tracker = new QueryTracker(options);
            
            Assert.Throws<ArgumentNullException>(() => tracker.TrackExecution(null!));
        }

        [Fact]
        public void TrackExecution_ThrowOnDetection_ThrowsException()
        {
            var options = new NPlusOneGuardOptions { Threshold = 1, ThrowOnDetection = true };
            var tracker = new QueryTracker(options);
            string sql = "SELECT * FROM Table";

            Assert.Throws<NPlusOneDetectedException>(() => tracker.TrackExecution(sql));
        }
    }
}
