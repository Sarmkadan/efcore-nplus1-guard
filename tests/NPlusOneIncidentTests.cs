using EfCoreNPlusOneGuard;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EfCoreNPlusOneGuard.Tests
{
    public class NPlusOneIncidentTests
    {
        [Fact]
        public void SqlQuery_Happy_PATH()
        {
            // Arrange
            var incident = new NPlusOneIncident();
            incident.SqlQuery = "SELECT * FROM users";
            // Act
            var result = incident.SqlQuery;
            // Assert
            Assert.Equal("SELECT * FROM users", result);
        }

        [Fact]
        public void Count_HAPPY_PATH()
        {
            // Arrange
            var incident = new NPlusOneIncident();
            incident.Count = 5;
            // Act
            var result = incident.Count;
            // Assert
            Assert.Equal(5, result);
        }

        [Fact]
        public void Severity_HAPPY_PATH()
        {
            // Arrange
            var incident = new NPlusOneIncident();
            incident.Severity = NPlusOneSeverity.Medium;
            // Act
            var result = incident.Severity;
            // Assert
            Assert.Equal(NPlusOneSeverity.Medium, result);
        }

        [Fact]
        public void StackTrace_HAPPY_PATH()
        {
            // Arrange
            var incident = new NPlusOneIncident();
            incident.StackTrace = "   at System.Data.SqlClient.TdsParserStateObject.Equals(TdsParserStateObject, TdsParserStateObject) \n   at System.Data.SqlClient.TdsParserStateObject..ctor(TdsParserStateObject*)";
            // Act
            var result = incident.StackTrace;
            // Assert
            Assert.Equal("   at System.Data.SqlClient.TdsParserStateObject.Equals(TdsParserStateObject, TdsParserStateObject) \n   at System.Data.SqlClient.TdsParserStateObject..ctor(TdsParserStateObject*)", result);
        }

        [Fact]
        public void CallSite_HAPPY_PATH()
        {
            // Arrange
            var incident = new NPlusOneIncident();
            incident.CallSite = "MyMethod";
            // Act
            var result = incident.CallSite;
            // Assert
            Assert.Equal("MyMethod", result);
        }
    }
}