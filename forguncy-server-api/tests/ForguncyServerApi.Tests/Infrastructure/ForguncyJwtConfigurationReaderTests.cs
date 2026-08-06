using System.Reflection;
using System.Runtime.ExceptionServices;
using Xunit;

namespace ForguncyServerApi.Tests.Infrastructure;

public sealed class ForguncyJwtConfigurationReaderTests
{
    private const string InvalidConfigurationMessage =
        "The Forguncy JWT configuration is invalid.";

    [Fact]
    public void ReadOrCreate_returns_existing_values_without_writes()
    {
        WithReader(readerType =>
        {
            var fake = CapturingDataAccess.Create(new Dictionary<string, Dictionary<string, object>?>
            {
                ["FGC_JWT_SIGNING_KEY"] = Row(1, new string('k', 32)),
                ["FGC_JWT_ISSUER"] = Row(2, "existing-issuer"),
                ["FGC_JWT_EXPIRES_MINUTES"] = Row(3, "15")
            });

            var values = ReadOrCreate(readerType, fake.DataAccess);

            Assert.Equal(new string('k', 32), values["FGC_JWT_SIGNING_KEY"]);
            Assert.Equal("existing-issuer", values["FGC_JWT_ISSUER"]);
            Assert.Equal("15", values["FGC_JWT_EXPIRES_MINUTES"]);
            Assert.Equal(
                new[] { "FGC_JWT_SIGNING_KEY", "FGC_JWT_ISSUER", "FGC_JWT_EXPIRES_MINUTES" },
                fake.ReadItems);
            Assert.Empty(fake.Additions);
            Assert.Empty(fake.Updates);
        });
    }

    [Fact]
    public void ReadOrCreate_inserts_generated_values_for_missing_rows()
    {
        WithReader(readerType =>
        {
            var fake = CapturingDataAccess.Create(
                new Dictionary<string, Dictionary<string, object>?>());

            var values = ReadOrCreate(readerType, fake.DataAccess);

            Assert.Equal(3, fake.Additions.Count);
            Assert.Empty(fake.Updates);
            Assert.Equal("60", values["FGC_JWT_EXPIRES_MINUTES"]);

            var signingKey = values["FGC_JWT_SIGNING_KEY"];
            Assert.NotNull(signingKey);
            Assert.Equal(32, Convert.FromBase64String(signingKey!).Length);

            var issuer = values["FGC_JWT_ISSUER"];
            Assert.NotNull(issuer);
            Assert.StartsWith("forguncy-server-api-", issuer, StringComparison.Ordinal);
            var suffix = issuer!.Substring("forguncy-server-api-".Length);
            Assert.Equal(32, suffix.Length);
            Assert.Equal(suffix.ToLowerInvariant(), suffix);

            Assert.Equal(values["FGC_JWT_SIGNING_KEY"], fake.Additions.Single(
                addition => addition.Item == "FGC_JWT_SIGNING_KEY").Value);
            Assert.Equal(values["FGC_JWT_ISSUER"], fake.Additions.Single(
                addition => addition.Item == "FGC_JWT_ISSUER").Value);
            Assert.Equal("60", fake.Additions.Single(
                addition => addition.Item == "FGC_JWT_EXPIRES_MINUTES").Value);
        });
    }

    [Fact]
    public void ReadOrCreate_treats_an_empty_sdk_row_as_a_missing_row()
    {
        WithReader(readerType =>
        {
            var fake = CapturingDataAccess.Create(new Dictionary<string, Dictionary<string, object>?>
            {
                ["FGC_JWT_SIGNING_KEY"] = new Dictionary<string, object>()
            });

            var values = ReadOrCreate(readerType, fake.DataAccess);

            Assert.Equal(3, fake.Additions.Count);
            Assert.Equal("60", values["FGC_JWT_EXPIRES_MINUTES"]);
        });
    }

    [Fact]
    public void ReadOrCreate_updates_blank_rows_by_existing_id()
    {
        WithReader(readerType =>
        {
            var fake = CapturingDataAccess.Create(new Dictionary<string, Dictionary<string, object>?>
            {
                ["FGC_JWT_SIGNING_KEY"] = Row(11, " "),
                ["FGC_JWT_ISSUER"] = Row(12, null),
                ["FGC_JWT_EXPIRES_MINUTES"] = Row(13, "\t")
            });

            var values = ReadOrCreate(readerType, fake.DataAccess);

            Assert.Empty(fake.Additions);
            Assert.Equal(3, fake.Updates.Count);
            Assert.Equal(11, fake.Updates.Single(update => update.Item == "FGC_JWT_SIGNING_KEY").Id);
            Assert.Equal(12, fake.Updates.Single(update => update.Item == "FGC_JWT_ISSUER").Id);
            Assert.Equal(13, fake.Updates.Single(update => update.Item == "FGC_JWT_EXPIRES_MINUTES").Id);
            Assert.All(fake.Updates, update => Assert.Equal(new[] { "value" }, update.UpdatedColumns));
            Assert.Equal("60", values["FGC_JWT_EXPIRES_MINUTES"]);
        });
    }

    [Fact]
    public void ReadOrCreate_does_not_generate_or_write_random_values_again_after_persistence()
    {
        WithReader(readerType =>
        {
            var fake = CapturingDataAccess.Create(
                new Dictionary<string, Dictionary<string, object>?>());

            var first = ReadOrCreate(readerType, fake.DataAccess);
            var second = ReadOrCreate(readerType, fake.DataAccess);

            Assert.Equal(first["FGC_JWT_SIGNING_KEY"], second["FGC_JWT_SIGNING_KEY"]);
            Assert.Equal(first["FGC_JWT_ISSUER"], second["FGC_JWT_ISSUER"]);
            Assert.Equal("60", second["FGC_JWT_EXPIRES_MINUTES"]);
            Assert.Equal(3, fake.Additions.Count);
            Assert.Empty(fake.Updates);
        });
    }

    [Fact]
    public void ReadOrCreate_throws_a_fixed_error_when_a_config_value_has_an_invalid_type()
    {
        WithReader(readerType =>
        {
            var fake = CapturingDataAccess.Create(new Dictionary<string, Dictionary<string, object>?>
            {
                ["FGC_JWT_SIGNING_KEY"] = Row(1, 42)
            });

            var exception = Assert.Throws<InvalidOperationException>(
                () => ReadOrCreate(readerType, fake.DataAccess));

            Assert.Equal(InvalidConfigurationMessage, exception.Message);
        });
    }

    [Fact]
    public void ReadOrCreate_throws_a_fixed_error_when_a_blank_row_has_no_id()
    {
        WithReader(readerType =>
        {
            var fake = CapturingDataAccess.Create(new Dictionary<string, Dictionary<string, object>?>
            {
                ["FGC_JWT_SIGNING_KEY"] = new Dictionary<string, object> { ["value"] = " " }
            });

            var exception = Assert.Throws<InvalidOperationException>(
                () => ReadOrCreate(readerType, fake.DataAccess));

            Assert.Equal(InvalidConfigurationMessage, exception.Message);
            Assert.Empty(fake.Additions);
            Assert.Empty(fake.Updates);
        });
    }

    [Fact]
    public void ReadOrCreate_hides_sdk_read_and_write_failures()
    {
        WithReader(readerType =>
        {
            const string sensitiveDetail = "Server=should-not-escape;Password=should-not-escape;";
            var readFailure = CapturingDataAccess.Create(
                new Dictionary<string, Dictionary<string, object>?>());
            readFailure.ReadException = new InvalidOperationException(sensitiveDetail);

            var readException = Assert.Throws<InvalidOperationException>(
                () => ReadOrCreate(readerType, readFailure.DataAccess));
            Assert.Equal(InvalidConfigurationMessage, readException.Message);
            Assert.DoesNotContain(sensitiveDetail, readException.Message);

            var writeFailure = CapturingDataAccess.Create(
                new Dictionary<string, Dictionary<string, object>?>());
            writeFailure.WriteException = new InvalidOperationException(sensitiveDetail);

            var writeException = Assert.Throws<InvalidOperationException>(
                () => ReadOrCreate(readerType, writeFailure.DataAccess));
            Assert.Equal(InvalidConfigurationMessage, writeException.Message);
            Assert.DoesNotContain(sensitiveDetail, writeException.Message);
        });
    }

    [Fact]
    public void ReadOrCreate_falls_back_to_an_odata_item_query_when_single_row_lookup_fails()
    {
        WithReader(readerType =>
        {
            var fake = CapturingDataAccess.Create(new Dictionary<string, Dictionary<string, object>?>
            {
                ["FGC_JWT_SIGNING_KEY"] = Row(1, new string('k', 32)),
                ["FGC_JWT_ISSUER"] = Row(2, "existing-issuer"),
                ["FGC_JWT_EXPIRES_MINUTES"] = Row(3, "15")
            });
            fake.PrimaryKeyReadException = new InvalidOperationException("item is not a primary key");

            var values = ReadOrCreate(readerType, fake.DataAccess);

            Assert.Equal(new string('k', 32), values["FGC_JWT_SIGNING_KEY"]);
            Assert.Equal("existing-issuer", values["FGC_JWT_ISSUER"]);
            Assert.Equal("15", values["FGC_JWT_EXPIRES_MINUTES"]);
            Assert.Equal(3, fake.ODataPaths.Count);
            Assert.All(fake.ODataPaths, path => Assert.Contains("$filter=item eq '", path));
            Assert.Empty(fake.Additions);
            Assert.Empty(fake.Updates);
        });
    }

    [Fact]
    public void ReadOrCreate_inserts_after_odata_confirms_that_the_item_is_missing()
    {
        WithReader(readerType =>
        {
            var fake = CapturingDataAccess.Create(
                new Dictionary<string, Dictionary<string, object>?>());
            fake.PrimaryKeyReadException = new InvalidOperationException("item is not a primary key");

            var values = ReadOrCreate(readerType, fake.DataAccess);

            Assert.Equal(3, fake.ODataPaths.Count);
            Assert.Equal(3, fake.Additions.Count);
            Assert.Empty(fake.Updates);
            Assert.Equal("60", values["FGC_JWT_EXPIRES_MINUTES"]);
        });
    }

    private static Dictionary<string, object> Row(int id, object? value) => new()
    {
        ["id"] = id,
        ["value"] = value!
    };

    private static void WithReader(Action<Type> action)
    {
        ResolveEventHandler handler = ResolveForguncyServerApi;
        AppDomain.CurrentDomain.AssemblyResolve += handler;
        try
        {
            var readerType = Assembly.Load("ForguncyServerApi")
                .GetType("ForguncyServerApi.Infrastructure.ForguncyJwtConfigurationReader");
            Assert.NotNull(readerType);
            action(readerType!);
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= handler;
        }
    }

    private static IReadOnlyDictionary<string, string?> ReadOrCreate(Type readerType, object dataAccess)
    {
        var method = readerType.GetMethod("ReadOrCreate", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);

        try
        {
            return Assert.IsAssignableFrom<IReadOnlyDictionary<string, string?>>(
                method!.Invoke(null, new[] { dataAccess }));
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

    public class CapturingDataAccess : DispatchProxy
    {
        private int nextId = 100;

        public Dictionary<string, Dictionary<string, object>?> Rows { get; } = new(StringComparer.Ordinal);

        public object DataAccess => this;

        public List<string> ReadItems { get; } = new();

        public List<WriteOperation> Additions { get; } = new();

        public List<UpdateOperation> Updates { get; } = new();

        public List<string> ODataPaths { get; } = new();

        public Exception? ReadException { get; set; }

        public Exception? PrimaryKeyReadException { get; set; }

        public Exception? WriteException { get; set; }

        public static CapturingDataAccess Create(
            Dictionary<string, Dictionary<string, object>?> rows)
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
            foreach (var pair in rows)
            {
                proxy.Rows[pair.Key] = pair.Value;
            }

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
                var item = filter.GetType().GetProperty("Value")?.GetValue(filter)?.ToString();
                Assert.Equal("config", tableName);
                Assert.Equal("item", filter.GetType().GetProperty("ColumnName")?.GetValue(filter));
                ReadItems.Add(item!);
                if (PrimaryKeyReadException is not null)
                {
                    throw PrimaryKeyReadException;
                }

                if (ReadException is not null)
                {
                    throw ReadException;
                }

                return item is not null && Rows.TryGetValue(item, out var row) ? row : null;
            }

            if (targetMethod?.Name == "GetTableData"
                && args is { Length: 1 }
                && args[0] is string odataPath)
            {
                ODataPaths.Add(odataPath);
                if (ReadException is not null)
                {
                    throw ReadException;
                }

                const string marker = "item eq '";
                var markerStart = odataPath.IndexOf(marker, StringComparison.Ordinal);
                var itemStart = markerStart < 0 ? -1 : markerStart + marker.Length;
                var itemEnd = itemStart < 0 ? -1 : odataPath.IndexOf('\'', itemStart);
                var item = itemStart >= 0 && itemEnd > itemStart
                    ? odataPath.Substring(itemStart, itemEnd - itemStart)
                    : null;
                if (item is not null
                    && Rows.TryGetValue(item, out var row)
                    && row is not null
                    && row.Count > 0)
                {
                    return new[] { row };
                }

                return Array.Empty<Dictionary<string, object>>();
            }

            if (targetMethod?.Name == "AddTableData"
                && args is { Length: 2 }
                && args[0] is string addTable
                && args[1] is Dictionary<string, object> addition)
            {
                Assert.Equal("config", addTable);
                if (WriteException is not null)
                {
                    throw WriteException;
                }

                var item = addition["item"].ToString()!;
                var value = addition["value"].ToString()!;
                Additions.Add(new WriteOperation(item, value));
                Rows[item] = new Dictionary<string, object>
                {
                    ["id"] = nextId++,
                    ["item"] = item,
                    ["value"] = value
                };
                return null;
            }

            if (targetMethod?.Name == "UpdateTableData"
                && args is { Length: 3 }
                && args[0] is string updateTable
                && args[1] is not null
                && args[2] is Dictionary<string, object> update)
            {
                Assert.Equal("config", updateTable);
                if (WriteException is not null)
                {
                    throw WriteException;
                }

                var filter = args[1]!;
                var id = Convert.ToInt32(filter.GetType().GetProperty("Value")?.GetValue(filter));
                var item = Rows.Single(pair => pair.Value is not null
                        && Convert.ToInt32(pair.Value["id"]) == id)
                    .Key;
                var row = Rows[item]!;
                foreach (var pair in update)
                {
                    row[pair.Key] = pair.Value;
                }

                Updates.Add(new UpdateOperation(
                    item,
                    id,
                    update.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray(),
                    update["value"].ToString()!));
                return null;
            }

            throw new NotSupportedException(targetMethod?.Name);
        }

        public sealed record WriteOperation(string Item, string Value);

        public sealed record UpdateOperation(
            string Item,
            int Id,
            IReadOnlyList<string> UpdatedColumns,
            string Value);
    }
}
