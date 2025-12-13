using FluentAssertions;
using NoteBookmark.Domain;
using Xunit;

namespace NoteBookmark.Api.Tests.Domain;

public class SearchCriteriasTests
{
    [Fact]
    public void Constructor_ShouldSetSearchPrompt()
    {
        // Arrange
        var searchPrompt = "Find articles about {topic} from the last week";

        // Act
        var criterias = new SearchCriterias(searchPrompt);

        // Assert
        var result = criterias.GetSearchPrompt();
        result.Should().Contain("Find articles about");
    }

    [Fact]
    public void GetSplittedAllowedDomains_ShouldReturnNull_WhenAllowedDomainsIsNull()
    {
        // Arrange
        var criterias = new SearchCriterias("test") { AllowedDomains = null };

        // Act
        var result = criterias.GetSplittedAllowedDomains();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetSplittedAllowedDomains_ShouldSplitAndTrim_WhenAllowedDomainsProvided()
    {
        // Arrange
        var criterias = new SearchCriterias("test")
        {
            AllowedDomains = "example.com, test.com , another.com"
        };

        // Act
        var result = criterias.GetSplittedAllowedDomains();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain("example.com");
        result.Should().Contain("test.com");
        result.Should().Contain("another.com");
    }

    [Fact]
    public void GetSplittedAllowedDomains_ShouldHandleSingleDomain()
    {
        // Arrange
        var criterias = new SearchCriterias("test") { AllowedDomains = "example.com" };

        // Act
        var result = criterias.GetSplittedAllowedDomains();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.Should().Contain("example.com");
    }

    [Fact]
    public void GetSplittedBlockedDomains_ShouldReturnNull_WhenBlockedDomainsIsNull()
    {
        // Arrange
        var criterias = new SearchCriterias("test") { BlockedDomains = null };

        // Act
        var result = criterias.GetSplittedBlockedDomains();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetSplittedBlockedDomains_ShouldSplitAndTrim_WhenBlockedDomainsProvided()
    {
        // Arrange
        var criterias = new SearchCriterias("test")
        {
            BlockedDomains = "spam.com,  bad.com, malicious.com  "
        };

        // Act
        var result = criterias.GetSplittedBlockedDomains();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain("spam.com");
        result.Should().Contain("bad.com");
        result.Should().Contain("malicious.com");
    }

    [Fact]
    public void GetSearchPrompt_ShouldReplaceTopicPlaceholder()
    {
        // Arrange
        var criterias = new SearchCriterias("Find articles about {topic}")
        {
            SearchTopic = "Azure DevOps"
        };

        // Act
        var result = criterias.GetSearchPrompt();

        // Assert
        result.Should().Be("Find articles about  Azure DevOps ");
    }

    [Fact]
    public void GetSearchPrompt_ShouldHandleNullSearchTopic()
    {
        // Arrange
        var criterias = new SearchCriterias("Find articles about {topic}") { SearchTopic = null };

        // Act
        var result = criterias.GetSearchPrompt();

        // Assert
        result.Should().Be("Find articles about   ");
    }

    [Fact]
    public void GetSearchPrompt_ShouldHandleEmptySearchTopic()
    {
        // Arrange
        var criterias = new SearchCriterias("Find articles about {topic}") { SearchTopic = "" };

        // Act
        var result = criterias.GetSearchPrompt();

        // Assert
        result.Should().Be("Find articles about   ");
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        // Arrange
        var criterias = new SearchCriterias("test");

        // Act
        criterias.SearchTopic = "Kubernetes";
        criterias.AllowedDomains = "k8s.io";
        criterias.BlockedDomains = "spam.com";

        // Assert
        criterias.SearchTopic.Should().Be("Kubernetes");
        criterias.AllowedDomains.Should().Be("k8s.io");
        criterias.BlockedDomains.Should().Be("spam.com");
    }
}
