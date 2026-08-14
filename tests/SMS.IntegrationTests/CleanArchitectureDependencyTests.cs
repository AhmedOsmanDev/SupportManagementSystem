using FluentAssertions;

namespace SMS.IntegrationTests;

public sealed class CleanArchitectureDependencyTests
{
    [Fact]
    public void ApiAssembly_DoesNotReferenceDomainAssembly()
    {
        typeof(Program).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Should().NotContain("SMS.Domain");
    }
}
