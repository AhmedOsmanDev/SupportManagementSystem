using FluentAssertions;
using SMS.Domain;

namespace SMS.UnitTests;

public sealed class TimeEntryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1441)]
    public void Create_WithOutOfRangeDuration_Throws(int durationMinutes)
    {
        var act = () => TimeEntry.Create(
            "TKT-000001",
            Guid.NewGuid(),
            DateTime.UtcNow,
            durationMinutes,
            "Investigated issue");

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("durationMinutes");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(1440)]
    public void Create_WithValidDuration_PreservesMinutes(int durationMinutes)
    {
        var entry = TimeEntry.Create(
            "TKT-000001",
            Guid.NewGuid(),
            DateTime.UtcNow,
            durationMinutes,
            "  Investigated issue  ");

        entry.DurationMinutes.Should().Be(durationMinutes);
        entry.Description.Should().Be("Investigated issue");
    }

    [Fact]
    public void Create_NormalizesWorkDateToUtcDate()
    {
        var workDate = new DateTime(2026, 8, 13, 18, 42, 30, DateTimeKind.Local);

        var entry = TimeEntry.Create(
            "TKT-000001",
            Guid.NewGuid(),
            workDate,
            15,
            "Investigated issue");

        entry.WorkDate.Should().Be(new DateTime(2026, 8, 13, 0, 0, 0, DateTimeKind.Utc));
        entry.WorkDate.Kind.Should().Be(DateTimeKind.Utc);
    }
}

