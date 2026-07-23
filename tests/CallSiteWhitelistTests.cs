using System;
using Xunit;
using EfCoreNPlusOneGuard;

namespace EfCoreNPlusOneGuard.Tests
{
    public class CallSiteWhitelistTests
    {
        [Fact] public void Constructor_CreatesEmptyWhitelist() =>
            Assert.Equal(0, new CallSiteWhitelist().Count);

        [Fact] public void Add_WithValidTypeName_AddsExactEntry()
        {
            var wl = new CallSiteWhitelist();
            wl.Add("Namespace.TypeName");
            Assert.Equal(1, wl.Count);
        }

        [Fact] public void Add_WithValidTypeNameAndMethodName_AddsExactEntry()
        {
            var wl = new CallSiteWhitelist();
            wl.Add("Namespace.TypeName", "MethodName");
            Assert.Equal(1, wl.Count);
        }

        [Fact] public void Add_WithNullTypeName_Throws()
        {
            var wl = new CallSiteWhitelist();
            var ex = Assert.Throws<ArgumentException>(() => wl.Add(null!, null));
            Assert.Equal("Type name cannot be null or whitespace. (Parameter 'typeName')", ex.Message);
        }

        [Fact] public void Add_WithEmptyTypeName_Throws()
        {
            var wl = new CallSiteWhitelist();
            var ex = Assert.Throws<ArgumentException>(() => wl.Add(string.Empty, null));
            Assert.Equal("Type name cannot be null or whitespace. (Parameter 'typeName')", ex.Message);
        }

        [Fact] public void Add_WithWhitespaceTypeName_Throws()
        {
            var wl = new CallSiteWhitelist();
            var ex = Assert.Throws<ArgumentException>(() => wl.Add("   ", null));
            Assert.Equal("Type name cannot be null or whitespace. (Parameter 'typeName')", ex.Message);
        }

        [Fact] public void AddPattern_WithValidPattern_AddsPatternEntry()
        {
            var wl = new CallSiteWhitelist();
            wl.AddPattern("Namespace.*");
            Assert.Equal(1, wl.Count);
        }

        [Fact] public void AddPattern_WithNullPattern_Throws()
        {
            var wl = new CallSiteWhitelist();
            var ex = Assert.Throws<ArgumentException>(() => wl.AddPattern(null!));
            Assert.Equal("Pattern cannot be null or whitespace. (Parameter 'wildcardPattern')", ex.Message);
        }

        [Fact] public void AddPattern_WithEmptyPattern_Throws()
        {
            var wl = new CallSiteWhitelist();
            var ex = Assert.Throws<ArgumentException>(() => wl.AddPattern(string.Empty));
            Assert.Equal("Pattern cannot be null or whitespace. (Parameter 'wildcardPattern')", ex.Message);
        }

        [Fact] public void AddPattern_WithWhitespacePattern_Throws()
        {
            var wl = new CallSiteWhitelist();
            var ex = Assert.Throws<ArgumentException>(() => wl.AddPattern("   "));
            Assert.Equal("Pattern cannot be null or whitespace. (Parameter 'wildcardPattern')", ex.Message);
        }

        [Fact] public void IsWhitelisted_WithNull_ReturnsFalse() =>
            Assert.False(new CallSiteWhitelist().IsWhitelisted(null));

        [Fact] public void IsWhitelisted_WithEmpty_ReturnsFalse() =>
            Assert.False(new CallSiteWhitelist().IsWhitelisted(string.Empty));

        [Fact] public void IsWhitelisted_WithNonAtLine_ReturnsFalse()
        {
            var wl = new CallSiteWhitelist();
            wl.Add("Namespace.TypeName");
            Assert.False(wl.IsWhitelisted("Not a stack trace"));
        }

        [Fact] public void IsWhitelisted_WithExactTypeMatch_ReturnsTrue()
        {
            var wl = new CallSiteWhitelist();
            wl.Add("EfCoreNPlusOneGuard.Services.MyService");
            Assert.True(wl.IsWhitelisted("at EfCoreNPlusOneGuard.Services.MyService.GetData (at C:/file.cs:10)"));
        }

        [Fact] public void IsWhitelisted_WithExactTypeAndMethodMatch_ReturnsTrue()
        {
            var wl = new CallSiteWhitelist();
            wl.Add("EfCoreNPlusOneGuard.Services.MyService", "GetData");
            Assert.True(wl.IsWhitelisted("at EfCoreNPlusOneGuard.Services.MyService.GetData (at C:/file.cs:10)"));
        }

        [Fact] public void IsWhitelisted_WithExactTypeAndDifferentMethod_ReturnsFalse()
        {
            var wl = new CallSiteWhitelist();
            wl.Add("EfCoreNPlusOneGuard.Services.MyService", "GetData");
            Assert.False(wl.IsWhitelisted("at EfCoreNPlusOneGuard.Services.MyService.SaveData (at C:/file.cs:15)"));
        }

        [Fact] public void IsWhitelisted_WithExactTypeAndNullMethod_ReturnsTrue()
        {
            var wl = new CallSiteWhitelist();
            wl.Add("EfCoreNPlusOneGuard.Services.MyService", null);
            Assert.True(wl.IsWhitelisted("at EfCoreNPlusOneGuard.Services.MyService.GetData (at C:/file.cs:10)"));
            Assert.True(wl.IsWhitelisted("at EfCoreNPlusOneGuard.Services.MyService.SaveData (at C:/file.cs:15)"));
        }

        [Fact] public void IsWhitelisted_WithPatternMatch_ReturnsTrue()
        {
            var wl = new CallSiteWhitelist();
            wl.AddPattern("EfCoreNPlusOneGuard.*");
            Assert.True(wl.IsWhitelisted("at EfCoreNPlusOneGuard.Services.MyService.GetData (at C:/file.cs:10)"));
        }

        [Fact] public void IsWhitelisted_WithPatternPartialMatch_ReturnsTrue()
        {
            var wl = new CallSiteWhitelist();
            wl.AddPattern("*.Services.*");
            Assert.True(wl.IsWhitelisted("at EfCoreNPlusOneGuard.Services.MyService.GetData (at C:/file.cs:10)"));
        }

        [Fact] public void IsWhitelisted_WithPatternNoMatch_ReturnsFalse()
        {
            var wl = new CallSiteWhitelist();
            wl.AddPattern("EfCoreNPlusOneGuard.Controllers.*");
            Assert.False(wl.IsWhitelisted("at EfCoreNPlusOneGuard.Services.MyService.GetData (at C:/file.cs:10)"));
        }

        [Fact] public void IsWhitelisted_WithMultipleEntries_MatchesAny()
        {
            var wl = new CallSiteWhitelist();
            wl.Add("EfCoreNPlusOneGuard.Services.OtherService");
            wl.AddPattern("EfCoreNPlusOneGuard.*");
            Assert.True(wl.IsWhitelisted("at EfCoreNPlusOneGuard.Services.MyService.GetData (at C:/file.cs:10)"));
        }

        [Fact] public void IsWhitelisted_WithComplexStackTrace_ReturnsTrueWhenMatching()
        {
            var wl = new CallSiteWhitelist();
            wl.Add("EfCoreNPlusOneGuard.Services.MyService", "GetData");
            var trace = @"at EfCoreNPlusOneGuard.Services.OtherService.Process ()
at EfCoreNPlusOneGuard.Services.MyService.GetData (at C:/file.cs:10)
at System.Threading.Tasks.Task.<>c.b__100_0()";
            Assert.True(wl.IsWhitelisted(trace));
        }

        [Fact] public void IsWhitelisted_WithComplexStackTrace_ReturnsFalseWhenNoMatch()
        {
            var wl = new CallSiteWhitelist();
            wl.Add("EfCoreNPlusOneGuard.Services.OtherService");
            var trace = @"at EfCoreNPlusOneGuard.Controllers.HomeController.Index ()
at Microsoft.EntityFrameworkCore.Query.Internal.QueryCompiler.ExecuteAsync()
at System.Threading.Tasks.Task.<>c.b__100_0()";
            Assert.False(wl.IsWhitelisted(trace));
        }

        [Fact] public void Clear_WithEntries_RemovesAll()
        {
            var wl = new CallSiteWhitelist();
            wl.Add("Type1");
            wl.Add("Type2");
            wl.AddPattern("Pattern*");
            Assert.Equal(3, wl.Count);
            wl.Clear();
            Assert.Equal(0, wl.Count);
        }

        [Fact] public void Clear_WithEmpty_DoesNotThrow() =>
            new CallSiteWhitelist().Clear();

        [Fact] public void Count_WithMultipleAdds_ReturnsCorrectCount()
        {
            var wl = new CallSiteWhitelist();
            wl.Add("Type1");
            wl.Add("Type2", "Method");
            wl.AddPattern("Pattern*");
            Assert.Equal(3, wl.Count);
        }
    }
}
