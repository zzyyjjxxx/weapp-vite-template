using ForguncyServerApi.Domain;
using SqlSugar;

namespace ForguncyServerApi.Infrastructure;

public sealed class EnterpriseRepository : IEnterpriseRepository
{
    private readonly Func<SqlSugarClient> _clientFactory;

    public EnterpriseRepository(Func<SqlSugarClient> clientFactory)
    {
        _clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    }

    public async Task<EnterpriseProfile?> FindByCreditCodeAsync(string creditCode, CancellationToken cancellationToken)
    {
        if (creditCode is null)
        {
            throw new ArgumentNullException(nameof(creditCode));
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var client = _clientFactory();
        var row = await BuildLookupQuery(client, creditCode)
            .SingleAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return row is null
            ? null
            : new EnterpriseProfile
            {
                UserId = row.UserId,
                CreditCode = row.CreditCode,
                BusinessName = row.BusinessName,
                CountyName = row.CountyName,
                Region = row.Region
            };
    }

    private static ISugarQueryable<EnterpriseLookupRow> BuildLookupQuery(SqlSugarClient client, string creditCode) =>
        client.Queryable<EnterpriseRow>()
            .InnerJoin<RegionRow>((enterprise, region) => enterprise.CountyId == region.Id)
            .Where((enterprise, region) => enterprise.CreditCode == creditCode)
            .Select((enterprise, region) => new EnterpriseLookupRow
            {
                UserId = enterprise.Id,
                CreditCode = enterprise.CreditCode,
                BusinessName = enterprise.BusinessName,
                CountyName = region.Name,
                Region = enterprise.Region
            });

    [SugarTable("m_preliminary_list")]
    private sealed class EnterpriseRow
    {
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true)]
        public int Id { get; set; }

        [SugarColumn(ColumnName = "businessName")]
        public string BusinessName { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "creditCode")]
        public string CreditCode { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "county")]
        public string CountyId { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "region")]
        public string Region { get; set; } = string.Empty;
    }

    [SugarTable("yj_regioninfo")]
    private sealed class RegionRow
    {
        [SugarColumn(ColumnName = "id")]
        public string Id { get; set; } = string.Empty;

        [SugarColumn(ColumnName = "name")]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class EnterpriseLookupRow
    {
        public int UserId { get; set; }

        public string CreditCode { get; set; } = string.Empty;

        public string BusinessName { get; set; } = string.Empty;

        public string CountyName { get; set; } = string.Empty;

        public string Region { get; set; } = string.Empty;
    }
}
