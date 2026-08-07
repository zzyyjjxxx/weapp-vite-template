using ForguncyServerApi.Application;
using ForguncyServerApi.Domain;
using SqlSugar;

namespace ForguncyServerApi.Infrastructure;

public sealed class LandDemandRepository : ILandDemandRepository
{
    private readonly Func<SqlSugarClient> clientFactory;

    public LandDemandRepository(Func<SqlSugarClient> clientFactory)
    {
        this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
    }

    public async Task<LandDemandRecord?> FindByCreditCodeAsync(string creditCode, CancellationToken cancellationToken)
    {
        if (creditCode is null)
        {
            throw new ArgumentNullException(nameof(creditCode));
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var client = clientFactory();
        var record = await BuildLookupQuery(client, creditCode.Trim()).SingleAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return record;
    }

    public async Task<LandDemandRecord> InsertAsync(LandDemandRecord record, CancellationToken cancellationToken)
    {
        if (record is null)
        {
            throw new ArgumentNullException(nameof(record));
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var client = clientFactory();
        var inserted = await client.Insertable(record).ExecuteReturnEntityAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return inserted;
    }

    public async Task<bool> UpdateWritableFieldsAsync(
        string creditCode,
        LandDemandWriteRequest request,
        string updateTime,
        string updateUser,
        CancellationToken cancellationToken)
    {
        if (creditCode is null)
        {
            throw new ArgumentNullException(nameof(creditCode));
        }

        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();
        using var client = clientFactory();
        var affectedRows = await BuildUpdateCommand(client, creditCode.Trim(), request, updateTime, updateUser)
            .ExecuteCommandAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return affectedRows > 0;
    }

    private static ISugarQueryable<LandDemandRecord> BuildLookupQuery(SqlSugarClient client, string creditCode) =>
        client.Queryable<LandDemandRecord>()
            .Where(record => record.Creditcode == creditCode);

    private static IUpdateable<LandDemandRecord> BuildUpdateCommand(
        SqlSugarClient client,
        string creditCode,
        LandDemandWriteRequest request,
        string updateTime,
        string updateUser) =>
        client.Updateable<LandDemandRecord>()
            .SetColumns(record => new LandDemandRecord
            {
                Area = request.Area,
                BuildingArea = request.BuildingArea,
                ExpectPark = request.ExpectPark,
                ExpectTime = request.ExpectTime,
                IsDeploy = request.IsDeploy,
                DeployPark = request.DeployPark,
                IsSpecialuse = request.IsSpecialuse,
                DeployLandtype = request.DeployLandtype,
                DeployHeight = request.DeployHeight,
                DeployWeight = request.DeployWeight,
                Investment = request.Investment,
                ProjectHydm = request.ProjectHydm,
                Keyindustry = request.Keyindustry,
                Futureindustry = request.Futureindustry,
                PredYs = request.PredYs,
                PredTax = request.PredTax,
                PredRdex = request.PredRdex,
                PredUnitenergy = request.PredUnitenergy,
                Projectdata = request.Projectdata,
                IsFinancing = request.IsFinancing,
                FinancingMoney = request.FinancingMoney,
                FinancingTime = request.FinancingTime,
                Contact = request.Contact,
                Office = request.Office,
                Phone = request.Phone,
                Landusedemand = request.Landusedemand,
                Updatetime = updateTime,
                Updateuser = updateUser
            })
            .Where(record => record.Creditcode == creditCode);
}
