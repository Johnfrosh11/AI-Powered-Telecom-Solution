using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace NaijaShield.ArchitectureTests;

public class CleanArchitectureTests
{
    private const string DomainNs = "NaijaShield.Domain";
    private const string ApplicationNs = "NaijaShield.Application";
    private const string InfrastructureNs = "NaijaShield.Infrastructure";
    private const string ApiNs = "NaijaShield.Api";

    [Fact]
    public void Domain_Should_Not_Have_Dependencies_On_Other_Layers()
    {
        var result = Types.InAssembly(typeof(Domain.Common.Entity).Assembly)
            .Should()
            .NotHaveDependencyOnAny(ApplicationNs, InfrastructureNs, ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Domain layer must not depend on outer layers. Failing types: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InAssembly(typeof(Application.DependencyInjection).Assembly)
            .Should()
            .NotHaveDependencyOnAny(InfrastructureNs, ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Application must not reference Infrastructure/Api. Failing: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(typeof(Infrastructure.DependencyInjection).Assembly)
            .Should()
            .NotHaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void AggregateRoots_Should_Inherit_From_AggregateRoot()
    {
        var result = Types.InAssembly(typeof(Domain.Common.Entity).Assembly)
            .That()
            .ResideInNamespaceStartingWith(DomainNs + ".Aggregates")
            .And()
            .HaveNameEndingWith("Repository")
            .GetTypes();

        result.Should().BeEmpty("Repositories should not be in the Domain layer.");
    }

    [Fact]
    public void Controllers_Should_Be_In_Api_Layer()
    {
        var result = Types.InAssembly(typeof(Api.Hubs.FraudHub).Assembly)
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Controller")
            .Should()
            .ResideInNamespaceStartingWith(ApiNs + ".Controllers")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void CommandHandlers_Should_Be_In_Application_Layer()
    {
        var result = Types.InAssembly(typeof(Application.DependencyInjection).Assembly)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .ResideInNamespaceStartingWith(ApplicationNs)
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
