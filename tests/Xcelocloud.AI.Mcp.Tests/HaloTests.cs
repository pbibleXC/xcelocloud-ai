using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Xcelocloud.AI.Mcp;
using Xcelocloud.Service.Shared.Helpers;
using Xcelocloud.Service.Shared.Models;

namespace Xcelocloud.AI.Mcp.Tests;

[TestFixture]
public class HaloTests
{
    private Mock<IHaloQueryHelper> _mockQueryHelper = null!;
    private Mock<ILogger<Halo>> _mockLogger = null!;
    private Halo _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _mockQueryHelper = new Mock<IHaloQueryHelper>();
        _mockLogger = new Mock<ILogger<Halo>>();
        _sut = new Halo(_mockQueryHelper.Object, _mockLogger.Object);
    }

    #region GetAgents

    [Test]
    public async Task GetAgents_ReturnsSerializedAgentList_WhenAgentsExist()
    {
        // Arrange
        var agents = new List<HaloAgent>
        {
            new() { Id = 1, Name = "Alice Smith", Email = "alice@example.com", JobTitle = "Support Engineer", Team = "Tier 1", IsDisabled = false },
            new() { Id = 2, Name = "Bob Jones",  Email = "bob@example.com",   JobTitle = "Senior Engineer",   Team = "Tier 2", IsDisabled = false }
        };
        _mockQueryHelper.Setup(x => x.RetrieveAgentList()).ReturnsAsync(agents);

        // Act
        var result = await _sut.GetAgents(null!);
        var doc = JsonDocument.Parse(result);

        // Assert
        Assert.That(doc.RootElement.GetProperty("count").GetInt32(), Is.EqualTo(2));
        Assert.That(doc.RootElement.GetProperty("agents").GetArrayLength(), Is.EqualTo(2));
        Assert.That(doc.RootElement.TryGetProperty("description", out _), Is.True);
    }

    [Test]
    public async Task GetAgents_ReturnsZeroCount_WhenNoAgentsExist()
    {
        // Arrange
        _mockQueryHelper.Setup(x => x.RetrieveAgentList()).ReturnsAsync([]);

        // Act
        var result = await _sut.GetAgents(null!);
        var doc = JsonDocument.Parse(result);

        // Assert
        Assert.That(doc.RootElement.GetProperty("count").GetInt32(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetAgents_ReturnsErrorResponse_WhenExceptionOccurs()
    {
        // Arrange
        const string expectedMessage = "Connection timeout";
        _mockQueryHelper.Setup(x => x.RetrieveAgentList()).ThrowsAsync(new Exception(expectedMessage));

        // Act
        var result = await _sut.GetAgents(null!);
        var doc = JsonDocument.Parse(result);

        // Assert
        Assert.That(doc.RootElement.GetProperty("success").GetBoolean(), Is.False);
        Assert.That(doc.RootElement.GetProperty("error").GetString(), Is.EqualTo(expectedMessage));
    }

    [Test]
    public async Task GetAgents_LogsError_WhenExceptionOccurs()
    {
        // Arrange
        _mockQueryHelper.Setup(x => x.RetrieveAgentList()).ThrowsAsync(new Exception("boom"));

        // Act
        await _sut.GetAgents(null!);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to retrieve Halo agents")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetAgentRoles

    [Test]
    public async Task GetAgentRoles_ReturnsSerializedRoleList_WhenRolesExist()
    {
        // Arrange
        var roles = new List<HaloAgentRole>
        {
            new() { AgentId = 1, AgentName = "Alice Smith", RoleId = "10", RoleName = "First Response" },
            new() { AgentId = 2, AgentName = "Bob Jones",  RoleId = "11", RoleName = "Escalation"     }
        };
        _mockQueryHelper.Setup(x => x.RetrieveAgentRoleList()).ReturnsAsync(roles);

        // Act
        var result = await _sut.GetAgentRoles(null!);
        var doc = JsonDocument.Parse(result);

        // Assert
        Assert.That(doc.RootElement.GetProperty("count").GetInt32(), Is.EqualTo(2));
        Assert.That(doc.RootElement.GetProperty("agentRoles").GetArrayLength(), Is.EqualTo(2));
        Assert.That(doc.RootElement.TryGetProperty("description", out _), Is.True);
    }

    [Test]
    public async Task GetAgentRoles_ReturnsZeroCount_WhenNoRolesExist()
    {
        // Arrange
        _mockQueryHelper.Setup(x => x.RetrieveAgentRoleList()).ReturnsAsync([]);

        // Act
        var result = await _sut.GetAgentRoles(null!);
        var doc = JsonDocument.Parse(result);

        // Assert
        Assert.That(doc.RootElement.GetProperty("count").GetInt32(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetAgentRoles_ReturnsErrorResponse_WhenExceptionOccurs()
    {
        // Arrange
        const string expectedMessage = "Unauthorized";
        _mockQueryHelper.Setup(x => x.RetrieveAgentRoleList()).ThrowsAsync(new Exception(expectedMessage));

        // Act
        var result = await _sut.GetAgentRoles(null!);
        var doc = JsonDocument.Parse(result);

        // Assert
        Assert.That(doc.RootElement.GetProperty("success").GetBoolean(), Is.False);
        Assert.That(doc.RootElement.GetProperty("error").GetString(), Is.EqualTo(expectedMessage));
    }

    [Test]
    public async Task GetAgentRoles_LogsError_WhenExceptionOccurs()
    {
        // Arrange
        _mockQueryHelper.Setup(x => x.RetrieveAgentRoleList()).ThrowsAsync(new Exception("boom"));

        // Act
        await _sut.GetAgentRoles(null!);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to retrieve Halo agent roles")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetAgentUtilization

    private static ToolInvocationContext ContextWith(Dictionary<string, object> args) =>
        new() { Name = string.Empty, Arguments = args };

    [Test]
    public async Task GetAgentUtilization_ReturnsSerializedData_WhenUtilizationExists()
    {
        // Arrange
        var utilization = new List<HaloAgentUtilization>
        {
            new() { Id = 1, Name = "Alice Smith", BillableHours = 120d, NonBillableHours = 20d, TargetBillableHours = 130d, LeaveHours = 8d, UtilizationRate = 92.3d },
            new() { Id = 2, Name = "Bob Jones",   BillableHours = 95d,  NonBillableHours = 15d, TargetBillableHours = 130d, LeaveHours = 0d,  UtilizationRate = 73.1d }
        };
        _mockQueryHelper
            .Setup(x => x.RetrieveAgentUtilizationList(It.IsAny<List<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(utilization);

        // Act
        var result = await _sut.GetAgentUtilization(null!);
        var doc = JsonDocument.Parse(result);

        // Assert
        Assert.That(doc.RootElement.GetProperty("count").GetInt32(), Is.EqualTo(2));
        Assert.That(doc.RootElement.GetProperty("agentUtilization").GetArrayLength(), Is.EqualTo(2));
        Assert.That(doc.RootElement.TryGetProperty("description", out _), Is.True);
    }

    [Test]
    public async Task GetAgentUtilization_ReturnsZeroCount_WhenListIsEmpty()
    {
        // Arrange
        _mockQueryHelper
            .Setup(x => x.RetrieveAgentUtilizationList(It.IsAny<List<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetAgentUtilization(null!);
        var doc = JsonDocument.Parse(result);

        // Assert
        Assert.That(doc.RootElement.GetProperty("count").GetInt32(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetAgentUtilization_ReturnsErrorResponse_WhenExceptionOccurs()
    {
        // Arrange
        const string expectedMessage = "Service unavailable";
        _mockQueryHelper
            .Setup(x => x.RetrieveAgentUtilizationList(It.IsAny<List<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .ThrowsAsync(new Exception(expectedMessage));

        // Act
        var result = await _sut.GetAgentUtilization(null!);
        var doc = JsonDocument.Parse(result);

        // Assert
        Assert.That(doc.RootElement.GetProperty("success").GetBoolean(), Is.False);
        Assert.That(doc.RootElement.GetProperty("error").GetString(), Is.EqualTo(expectedMessage));
    }

    [Test]
    public async Task GetAgentUtilization_LogsError_WhenExceptionOccurs()
    {
        // Arrange
        _mockQueryHelper
            .Setup(x => x.RetrieveAgentUtilizationList(It.IsAny<List<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .ThrowsAsync(new Exception("boom"));

        // Act
        await _sut.GetAgentUtilization(null!);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to retrieve Halo agent utilization metrics")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task GetAgentUtilization_UsesDefaultDateRange_WhenContextIsNull()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        DateTimeOffset capturedStart = default;
        DateTimeOffset capturedEnd = default;
        _mockQueryHelper
            .Setup(x => x.RetrieveAgentUtilizationList(It.IsAny<List<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .Callback<List<long>, DateTimeOffset, DateTimeOffset>((_, s, e) => { capturedStart = s; capturedEnd = e; })
            .ReturnsAsync([]);

        // Act
        await _sut.GetAgentUtilization(null!);
        var after = DateTimeOffset.UtcNow;

        // Assert — startDate ≈ now-30d, endDate ≈ now
        Assert.That(capturedStart, Is.GreaterThanOrEqualTo(before.AddDays(-30)).And.LessThanOrEqualTo(after.AddDays(-30).AddSeconds(5)));
        Assert.That(capturedEnd, Is.GreaterThanOrEqualTo(before).And.LessThanOrEqualTo(after.AddSeconds(1)));
    }

    [Test]
    public async Task GetAgentUtilization_PassesAgentIds_WhenProvided()
    {
        // Arrange
        List<long> capturedIds = null!;
        _mockQueryHelper
            .Setup(x => x.RetrieveAgentUtilizationList(It.IsAny<List<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .Callback<List<long>, DateTimeOffset, DateTimeOffset>((ids, _, _) => capturedIds = ids)
            .ReturnsAsync([]);

        var context = ContextWith(new Dictionary<string, object>
        {
            [ToolsInformation.GetAgentUtilization.AgentIdsPropertyName] = "1, 2, 3"
        });

        // Act
        await _sut.GetAgentUtilization(context);

        // Assert
        Assert.That(capturedIds, Is.EquivalentTo(new List<long> { 1, 2, 3 }));
    }

    [Test]
    public async Task GetAgentUtilization_UsesProvidedDateRange_WhenDatesAreValid()
    {
        // Arrange
        DateTimeOffset capturedStart = default;
        DateTimeOffset capturedEnd = default;
        _mockQueryHelper
            .Setup(x => x.RetrieveAgentUtilizationList(It.IsAny<List<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .Callback<List<long>, DateTimeOffset, DateTimeOffset>((_, s, e) => { capturedStart = s; capturedEnd = e; })
            .ReturnsAsync([]);

        var context = ContextWith(new Dictionary<string, object>
        {
            [ToolsInformation.GetAgentUtilization.StartDatePropertyName] = "2026-01-01T00:00:00Z",
            [ToolsInformation.GetAgentUtilization.EndDatePropertyName]   = "2026-02-01T00:00:00Z"
        });

        // Act
        await _sut.GetAgentUtilization(context);

        // Assert
        Assert.That(capturedStart, Is.EqualTo(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));
        Assert.That(capturedEnd,   Is.EqualTo(new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    [Test]
    public async Task GetAgentUtilization_IgnoresInvalidAgentIds_WhenInputIsMalformed()
    {
        // Arrange
        List<long> capturedIds = null!;
        _mockQueryHelper
            .Setup(x => x.RetrieveAgentUtilizationList(It.IsAny<List<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .Callback<List<long>, DateTimeOffset, DateTimeOffset>((ids, _, _) => capturedIds = ids)
            .ReturnsAsync([]);

        var context = ContextWith(new Dictionary<string, object>
        {
            [ToolsInformation.GetAgentUtilization.AgentIdsPropertyName] = "abc, 2, , xyz, 5"
        });

        // Act
        await _sut.GetAgentUtilization(context);

        // Assert — only valid numeric IDs are passed through
        Assert.That(capturedIds, Is.EquivalentTo(new List<long> { 2, 5 }));
    }

    [Test]
    public async Task GetAgentUtilization_UsesDefaultDates_WhenDateStringsAreInvalid()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        DateTimeOffset capturedStart = default;
        DateTimeOffset capturedEnd = default;
        _mockQueryHelper
            .Setup(x => x.RetrieveAgentUtilizationList(It.IsAny<List<long>>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>()))
            .Callback<List<long>, DateTimeOffset, DateTimeOffset>((_, s, e) => { capturedStart = s; capturedEnd = e; })
            .ReturnsAsync([]);

        var context = ContextWith(new Dictionary<string, object>
        {
            [ToolsInformation.GetAgentUtilization.StartDatePropertyName] = "not-a-date",
            [ToolsInformation.GetAgentUtilization.EndDatePropertyName]   = "also-not-a-date"
        });

        // Act
        await _sut.GetAgentUtilization(context);
        var after = DateTimeOffset.UtcNow;

        // Assert — falls back to defaults
        Assert.That(capturedStart, Is.GreaterThanOrEqualTo(before.AddDays(-30)).And.LessThanOrEqualTo(after.AddDays(-30).AddSeconds(5)));
        Assert.That(capturedEnd,   Is.GreaterThanOrEqualTo(before).And.LessThanOrEqualTo(after.AddSeconds(1)));
    }

    #endregion
}
