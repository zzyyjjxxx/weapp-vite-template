using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;

namespace ForguncyServerApi.Tests.Infrastructure;

public sealed class ForguncyConfigConnectionStringReaderTests
{
    [Fact]
    public void ReadRequired_returns_the_value_from_the_ssl_config_row()
    {
        WithReader(readerType =>
        {
            var fake = CapturingDataAccess.Create(new Dictionary<string, object>
            {
                ["value"] = "Server=synthetic;Database=synthetic;"
            });

            var connectionString = ReadRequired(readerType, fake.DataAccess);

            Assert.Equal("Server=synthetic;Database=synthetic;", connectionString);
            Assert.Equal("config", fake.TableName);
            Assert.Equal("item", fake.FilterColumnName);
            Assert.Equal("ssl", fake.FilterValue);
        });
    }

    [Fact]
    public void ReadRequired_throws_a_fixed_configuration_error_when_the_ssl_config_row_is_missing()
    {
        WithReader(readerType =>
        {
            var fake = CapturingDataAccess.Create(row: null);

            var exception = Assert.Throws<InvalidOperationException>(
                () => ReadRequired(readerType, fake.DataAccess));

            Assert.Equal("The Forguncy authentication connection configuration is invalid.", exception.Message);
        });
    }

    [Fact]
    public void ReadRequired_throws_a_fixed_configuration_error_when_the_value_is_missing()
    {
        WithReader(readerType =>
        {
            var fake = CapturingDataAccess.Create(new Dictionary<string, object>());

            var exception = Assert.Throws<InvalidOperationException>(
                () => ReadRequired(readerType, fake.DataAccess));

            Assert.Equal("The Forguncy authentication connection configuration is invalid.", exception.Message);
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ReadRequired_throws_a_fixed_configuration_error_when_the_value_is_null_or_blank(string? value)
    {
        WithReader(readerType =>
        {
            var fake = CapturingDataAccess.Create(new Dictionary<string, object>
            {
                ["value"] = value!
            });

            var exception = Assert.Throws<InvalidOperationException>(
                () => ReadRequired(readerType, fake.DataAccess));

            Assert.Equal("The Forguncy authentication connection configuration is invalid.", exception.Message);
        });
    }

    [Fact]
    public void ReadRequired_throws_a_fixed_configuration_error_when_the_value_is_not_a_string()
    {
        WithReader(readerType =>
        {
            var fake = CapturingDataAccess.Create(new Dictionary<string, object>
            {
                ["value"] = 42
            });

            var exception = Assert.Throws<InvalidOperationException>(
                () => ReadRequired(readerType, fake.DataAccess));

            Assert.Equal("The Forguncy authentication connection configuration is invalid.", exception.Message);
        });
    }

    [Fact]
    public void ReadRequired_throws_a_fixed_configuration_error_when_the_sdk_query_fails()
    {
        WithReader(readerType =>
        {
            const string sensitiveDetail = "Server=should-not-escape;Password=should-not-escape;";
            var fake = CapturingDataAccess.CreateThrowing(new InvalidOperationException(sensitiveDetail));

            var exception = Assert.Throws<InvalidOperationException>(
                () => ReadRequired(readerType, fake.DataAccess));

            Assert.Equal("The Forguncy authentication connection configuration is invalid.", exception.Message);
            Assert.DoesNotContain(sensitiveDetail, exception.Message);
            Assert.Equal("config", fake.TableName);
            Assert.Equal("item", fake.FilterColumnName);
            Assert.Equal("ssl", fake.FilterValue);
        });
    }

    private static void WithReader(Action<Type> action)
    {
        ResolveEventHandler handler = ResolveForguncyServerApi;
        AppDomain.CurrentDomain.AssemblyResolve += handler;
        try
        {
            var readerType = Assembly.Load("ForguncyServerApi")
                .GetType("ForguncyServerApi.Infrastructure.ForguncyConfigConnectionStringReader");
            Assert.NotNull(readerType);
            action(readerType!);
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= handler;
        }
    }

    private static string ReadRequired(Type readerType, object dataAccess)
    {
        var method = readerType.GetMethod("ReadRequired", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);

        try
        {
            return Assert.IsType<string>(method!.Invoke(null, new[] { dataAccess }));
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static Assembly? ResolveForguncyServerApi(object? sender, ResolveEventArgs args)
    {
        var name = new AssemblyName(args.Name);
        return name.Name == "GrapeCity.Forguncy.ServerApi"
            ? Assembly.LoadFrom("D:\\Program Files\\Forguncy 8.0.4\\Website\\bin\\GrapeCity.Forguncy.ServerApi.dll")
            : null;
    }

    private class CapturingDataAccess : DispatchProxy
    {
        public Dictionary<string, object>? Row { get; private set; }

        public object DataAccess => this;

        public string? TableName { get; private set; }

        public string? FilterColumnName { get; private set; }

        public object? FilterValue { get; private set; }

        public Exception? QueryException { get; private set; }

        public static CapturingDataAccess Create(Dictionary<string, object>? row)
        {
            var dataAccessType = Assembly.Load("GrapeCity.Forguncy.ServerApi")
                .GetType("GrapeCity.Forguncy.ServerApi.IDataAccess", throwOnError: true)!;
            var create = typeof(DispatchProxy)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(method => method.Name == nameof(DispatchProxy.Create)
                    && method.IsGenericMethodDefinition
                    && method.GetGenericArguments().Length == 2);
            var proxy = (CapturingDataAccess)create
                .MakeGenericMethod(dataAccessType, typeof(CapturingDataAccess))
                .Invoke(null, null)!;
            proxy.Row = row;
            return proxy;
        }

        public static CapturingDataAccess CreateThrowing(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            var proxy = Create(row: null);
            proxy.QueryException = exception;
            return proxy;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "GetTableData"
                && args is { Length: 2 }
                && args[0] is string tableName
                && args[1] is not null)
            {
                var filter = args[1]!;
                TableName = tableName;
                FilterColumnName = filter.GetType().GetProperty("ColumnName")?.GetValue(filter) as string;
                FilterValue = filter.GetType().GetProperty("Value")?.GetValue(filter);
                if (QueryException is not null)
                {
                    throw QueryException;
                }

                return Row;
            }

            throw new NotSupportedException(targetMethod?.Name);
        }
    }
}
