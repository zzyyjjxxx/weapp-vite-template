using ForguncyServerApi.Domain;
using ForguncyServerApi.Infrastructure;
using MySql.Data.MySqlClient;

namespace ForguncyServerApi.Application;

public sealed class LandDemandService
{
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";

    private readonly EnterpriseService enterpriseService;
    private readonly ILandDemandRepository repository;
    private readonly Func<DateTimeOffset> clock;

    public LandDemandService(
        EnterpriseService enterpriseService,
        ILandDemandRepository repository,
        Func<DateTimeOffset> clock)
    {
        this.enterpriseService = enterpriseService ?? throw new ArgumentNullException(nameof(enterpriseService));
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<LandDemandOperationResult> GetAsync(EnterpriseIdentity identity, CancellationToken cancellationToken)
    {
        var enterprise = await enterpriseService.GetProfileAsync(identity, cancellationToken);
        if (enterprise is null)
        {
            return new(LandDemandOperationStatus.EnterpriseNotFound, null);
        }

        var record = await repository.FindByCreditCodeAsync(enterprise.CreditCode, cancellationToken);
        return record is null
            ? new(LandDemandOperationStatus.NotFound, null)
            : new(LandDemandOperationStatus.Success, MapResponse(record));
    }

    public async Task<LandDemandOperationResult> AddAsync(
        EnterpriseIdentity identity,
        LandDemandWriteRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedRequest = request.Normalize();
        var enterprise = await enterpriseService.GetProfileAsync(identity, cancellationToken);
        if (enterprise is null)
        {
            return new(LandDemandOperationStatus.EnterpriseNotFound, null);
        }

        var existing = await repository.FindByCreditCodeAsync(enterprise.CreditCode, cancellationToken);
        if (existing is not null)
        {
            return new(LandDemandOperationStatus.Exists, null);
        }

        if (LandDemandValidation.ValidateNormalized(normalizedRequest).Count > 0)
        {
            return new(LandDemandOperationStatus.InvalidRequest, null);
        }

        var timestamp = clock().ToString(TimestampFormat);
        var record = CreateRecord(enterprise, normalizedRequest, timestamp);

        try
        {
            var inserted = await repository.InsertAsync(record, cancellationToken);
            return new(LandDemandOperationStatus.Success, MapResponse(inserted));
        }
        catch (Exception ex) when (IsDuplicateKey(ex))
        {
            return new(LandDemandOperationStatus.Exists, null);
        }
    }

    public async Task<LandDemandOperationResult> UpdateAsync(
        EnterpriseIdentity identity,
        LandDemandWriteRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedRequest = request.Normalize();
        var enterprise = await enterpriseService.GetProfileAsync(identity, cancellationToken);
        if (enterprise is null)
        {
            return new(LandDemandOperationStatus.EnterpriseNotFound, null);
        }

        var existing = await repository.FindByCreditCodeAsync(enterprise.CreditCode, cancellationToken);
        if (existing is null)
        {
            return new(LandDemandOperationStatus.NotFound, null);
        }

        if (LandDemandValidation.ValidateNormalized(normalizedRequest).Count > 0)
        {
            return new(LandDemandOperationStatus.InvalidRequest, null);
        }

        var timestamp = clock().ToString(TimestampFormat);
        var updated = ApplyWritableFields(existing, normalizedRequest, timestamp, enterprise.CreditCode);
        var saved = await repository.UpdateWritableFieldsAsync(
            enterprise.CreditCode,
            normalizedRequest,
            timestamp,
            enterprise.CreditCode,
            cancellationToken);

        return saved
            ? new(LandDemandOperationStatus.Success, MapResponse(updated))
            : new(LandDemandOperationStatus.NotFound, null);
    }

    private static bool IsDuplicateKey(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is MySqlException { Number: 1062 })
            {
                return true;
            }
        }

        return false;
    }

    private static LandDemandRecord CreateRecord(
        EnterpriseProfile enterprise,
        LandDemandWriteRequest request,
        string timestamp) =>
        ApplyWritableFields(
            new LandDemandRecord
            {
                Businessname = enterprise.BusinessName,
                Creditcode = enterprise.CreditCode,
                County = enterprise.CountyName,
                Region = enterprise.Region
            },
            request,
            timestamp,
            enterprise.CreditCode);

    private static LandDemandRecord ApplyWritableFields(
        LandDemandRecord target,
        LandDemandWriteRequest request,
        string timestamp,
        string updateUser)
    {
        target.Area = request.Area;
        target.BuildingArea = request.BuildingArea;
        target.ExpectPark = request.ExpectPark;
        target.ExpectTime = request.ExpectTime;
        target.IsDeploy = request.IsDeploy;
        target.DeployPark = request.DeployPark;
        target.IsSpecialuse = request.IsSpecialuse;
        target.DeployLandtype = request.DeployLandtype;
        target.DeployHeight = request.DeployHeight;
        target.DeployWeight = request.DeployWeight;
        target.Investment = request.Investment;
        target.ProjectHydm = request.ProjectHydm;
        target.Keyindustry = request.Keyindustry;
        target.Futureindustry = request.Futureindustry;
        target.PredYs = request.PredYs;
        target.PredTax = request.PredTax;
        target.PredRdex = request.PredRdex;
        target.PredUnitenergy = request.PredUnitenergy;
        target.Projectdata = request.Projectdata;
        target.IsFinancing = request.IsFinancing;
        target.FinancingMoney = request.FinancingMoney;
        target.FinancingTime = request.FinancingTime;
        target.Contact = request.Contact;
        target.Office = request.Office;
        target.Phone = request.Phone;
        target.Landusedemand = request.Landusedemand;
        target.Updatetime = timestamp;
        target.Updateuser = updateUser;
        return target;
    }

    private static LandDemandResponse MapResponse(LandDemandRecord record) =>
        new()
        {
            Businessname = record.Businessname,
            Creditcode = record.Creditcode,
            County = record.County,
            Region = record.Region,
            Area = record.Area,
            BuildingArea = record.BuildingArea,
            ExpectPark = record.ExpectPark,
            ExpectTime = record.ExpectTime,
            IsDeploy = record.IsDeploy,
            DeployPark = record.DeployPark,
            IsSpecialuse = record.IsSpecialuse,
            DeployLandtype = record.DeployLandtype,
            DeployHeight = record.DeployHeight,
            DeployWeight = record.DeployWeight,
            Investment = record.Investment,
            ProjectHydm = record.ProjectHydm,
            Keyindustry = record.Keyindustry,
            Futureindustry = record.Futureindustry,
            PredYs = record.PredYs,
            PredTax = record.PredTax,
            PredRdex = record.PredRdex,
            PredUnitenergy = record.PredUnitenergy,
            Projectdata = record.Projectdata,
            IsFinancing = record.IsFinancing,
            FinancingMoney = record.FinancingMoney,
            FinancingTime = record.FinancingTime,
            Contact = record.Contact,
            Office = record.Office,
            Phone = record.Phone,
            Landusedemand = record.Landusedemand,
            Updatetime = record.Updatetime
        };
}
