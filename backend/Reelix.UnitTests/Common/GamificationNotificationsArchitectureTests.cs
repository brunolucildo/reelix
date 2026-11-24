using System.Reflection;
using Reelix.Application.Gamification;
using Reelix.Domain.Users;

namespace Reelix.UnitTests.Common;

/// <summary>
/// Guards the shape of the Gamification and Notifications modules after spec 0014 (R6, R7, R12,
/// G8 and G9): narrow ports, one <c>ExecuteAsync</c> per use case, one use case per handler and no
/// Domain type in a command, query, result or HTTP contract.
/// </summary>
public sealed class GamificationNotificationsArchitectureTests
{
    private static readonly Assembly ApplicationAssembly = typeof(GetGamificationDashboardUseCase).Assembly;
    private static readonly Assembly DomainAssembly = typeof(UserAccount).Assembly;
    private static readonly Assembly ApiAssembly = typeof(Program).Assembly;

    private static readonly string[] Modules = ["Gamification", "Notifications"];

    private static bool InModuleNamespace(Type type, string root) =>
        Modules.Any(module =>
            type.Namespace == $"{root}.{module}" ||
            type.Namespace is { } ns && ns.StartsWith($"{root}.{module}.", StringComparison.Ordinal));

    private static IEnumerable<Type> ApplicationTypes() =>
        ApplicationAssembly.GetTypes().Where(type => InModuleNamespace(type, "Reelix.Application"));

    [Fact]
    public void EveryPortHasAtMostFiveOperations()
    {
        var interfaces = ApplicationTypes().Where(type => type.IsInterface).ToArray();

        Assert.NotEmpty(interfaces);
        foreach (var port in interfaces)
        {
            var operations = port
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Count(method => !method.IsSpecialName);
            Assert.True(operations <= 5, $"{port.Name} has {operations} operations");
        }
    }

    [Fact]
    public void TheWideRepositoriesNoLongerExist()
    {
        var names = ApplicationAssembly.GetTypes().Select(type => type.Name).ToHashSet();

        Assert.DoesNotContain("IGamificationRepository", names);
        Assert.DoesNotContain("INotificationsRepository", names);
        Assert.DoesNotContain("IExpoPushClient", names);
        Assert.DoesNotContain("GamificationService", names);
        Assert.DoesNotContain("NotificationsService", names);
    }

    [Fact]
    public void EveryUseCaseHasExactlyOneExecuteMethod()
    {
        var useCases = ApplicationTypes()
            .Where(type => type.IsClass && type.Name.EndsWith("UseCase", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(14, useCases.Length);
        foreach (var useCase in useCases)
        {
            var methods = useCase
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .ToArray();
            var execute = Assert.Single(methods);
            Assert.Equal("ExecuteAsync", execute.Name);
        }
    }

    [Fact]
    public void NoCommandQueryOrResultExposesADomainType()
    {
        var shapes = ApplicationTypes()
            .Where(type => type.IsClass &&
                (type.Name.EndsWith("Command", StringComparison.Ordinal) ||
                 type.Name.EndsWith("Query", StringComparison.Ordinal) ||
                 type.Name.EndsWith("Result", StringComparison.Ordinal)))
            .ToArray();

        Assert.NotEmpty(shapes);
        foreach (var shape in shapes)
        {
            AssertNoDomainTypes(shape);
        }
    }

    [Fact]
    public void NoHttpContractExposesADomainType()
    {
        var contracts = ApiAssembly.GetTypes()
            .Where(type => type.IsClass &&
                type.Namespace is { } ns &&
                Modules.Any(module => ns == $"Reelix.Api.{module}.Contracts"))
            .ToArray();

        Assert.NotEmpty(contracts);
        foreach (var contract in contracts)
        {
            AssertNoDomainTypes(contract);
        }
    }

    [Fact]
    public void EveryHandlerInjectsExactlyOneUseCaseAndNoUseCaseIsSharedByTwoHandlers()
    {
        var handlers = ApiAssembly.GetTypes()
            .Where(type => type.Name.EndsWith("Handler", StringComparison.Ordinal) &&
                Modules.Any(module => type.Namespace == $"Reelix.Api.{module}.Handlers"))
            .ToArray();

        Assert.Equal(12, handlers.Length);
        var useCaseTypes = handlers.Select(handler =>
        {
            var handle = handler.GetMethod(
                "HandleAsync",
                BindingFlags.Public | BindingFlags.Static)!;
            return Assert.Single(
                handle.GetParameters().Select(parameter => parameter.ParameterType),
                type => type.Name.EndsWith("UseCase", StringComparison.Ordinal));
        }).ToArray();
        Assert.Equal(12, useCaseTypes.Distinct().Count());
    }

    private static void AssertNoDomainTypes(Type shape)
    {
        foreach (var member in shape.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            foreach (var type in Flatten(member.PropertyType))
            {
                Assert.False(
                    type.Assembly == DomainAssembly,
                    $"{shape.Name}.{member.Name} exposes the Domain type {type.Name}");
            }
        }
    }

    private static IEnumerable<Type> Flatten(Type type)
    {
        yield return type;
        if (type.IsArray)
        {
            foreach (var inner in Flatten(type.GetElementType()!))
            {
                yield return inner;
            }
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var inner in Flatten(argument))
                {
                    yield return inner;
                }
            }
        }
    }
}
