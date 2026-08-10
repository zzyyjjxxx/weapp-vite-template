using System.Reflection;
using ForguncyServerApi.Application;
using ForguncyServerApi.Domain;
using ForguncyServerApi.Infrastructure;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ForguncyServerApi.Tests.Application;

public sealed class LandDemandServiceTests
{
    [Fact]
    public void LandDemand_response_contract_exposes_only_the_whitelisted_properties()
    {
        var propertyNames = typeof(LandDemandResponse)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "Area",
                "BuildingArea",
                "Businessname",
                "Contact",
                "County",
                "Creditcode",
                "DeployHeight",
                "DeployLandtype",
                "DeployPark",
                "DeployWeight",
                "ExpectPark",
                "ExpectTime",
                "FinancingMoney",
                "FinancingTime",
                "Futureindustry",
                "Investment",
                "IsDeploy",
                "IsFinancing",
                "IsSpecialuse",
                "Keyindustry",
                "Landusedemand",
                "Office",
                "Phone",
                "PredRdex",
                "PredTax",
                "PredUnitenergy",
                "PredYs",
                "ProjectHydm",
                "Projectdata",
                "Region",
                "Updatetime"
            },
            propertyNames);
    }

    [Fact]
    public void LandDemand_response_serializes_with_the_exact_approved_snake_and_lower_case_keys()
    {
        var response = new LandDemandResponse
        {
            Businessname = "Synthetic Enterprise",
            Creditcode = "91330200SYNTHETIC",
            County = "Yinzhou",
            Region = "Shounan",
            Area = "80",
            BuildingArea = 1200.50m,
            ExpectPark = "Ningbo Industrial Park",
            ExpectTime = "2026-08",
            IsDeploy = "0",
            DeployPark = null,
            IsSpecialuse = "0",
            DeployLandtype = null,
            DeployHeight = 12.5m,
            DeployWeight = 2.5m,
            Investment = 6000m,
            ProjectHydm = "A0111",
            Keyindustry = "Synthetic Key Industry",
            Futureindustry = "Synthetic Future Industry",
            PredYs = 7000m,
            PredTax = 800m,
            PredRdex = 300m,
            PredUnitenergy = 15m,
            Projectdata = "Build a new production line.",
            IsFinancing = "没有",
            FinancingMoney = 99999999999999.999999m,
            FinancingTime = "2027-12",
            Contact = "Alice",
            Office = "General Manager",
            Phone = "13800000000",
            Landusedemand = "1",
            Updatetime = "2026-08-06 10:20:30"
        };

        var json = JsonConvert.SerializeObject(response);
        var propertyNames = JObject.Parse(json)
            .Properties()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "area",
                "building_area",
                "businessname",
                "contact",
                "county",
                "creditcode",
                "deploy_height",
                "deploy_landtype",
                "deploy_park",
                "deploy_weight",
                "expect_park",
                "expect_time",
                "financing_money",
                "financing_time",
                "futureindustry",
                "investment",
                "is_deploy",
                "is_financing",
                "is_specialuse",
                "keyindustry",
                "landusedemand",
                "office",
                "phone",
                "pred_rdex",
                "pred_tax",
                "pred_unitenergy",
                "pred_ys",
                "project_hydm",
                "projectdata",
                "region",
                "updatetime"
            },
            propertyNames);
        Assert.DoesNotContain("Businessname", json, StringComparison.Ordinal);
        Assert.DoesNotContain("BuildingArea", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAsync_returns_enterprise_not_found_when_the_authenticated_enterprise_profile_is_missing()
    {
        var enterpriseRepository = new StubEnterpriseRepository(null);
        var landDemandRepository = new StubLandDemandRepository();
        var service = CreateService(enterpriseRepository, landDemandRepository);

        var result = await service.GetAsync(
            new EnterpriseIdentity(7, "91330200MISSING"),
            CancellationToken.None);

        Assert.Equal(LandDemandOperationStatus.EnterpriseNotFound, result.Status);
        Assert.Null(result.Record);
        Assert.Equal("91330200MISSING", enterpriseRepository.LastCreditCode);
        Assert.Null(landDemandRepository.LastFindCreditCode);
    }

    [Fact]
    public async Task GetAsync_returns_not_found_when_the_authenticated_enterprise_has_no_land_demand_record()
    {
        var enterpriseRepository = new StubEnterpriseRepository(CreateEnterpriseProfile());
        var landDemandRepository = new StubLandDemandRepository();
        var service = CreateService(enterpriseRepository, landDemandRepository);

        var result = await service.GetAsync(
            new EnterpriseIdentity(7, "91330200SYNTHETIC"),
            CancellationToken.None);

        Assert.Equal(LandDemandOperationStatus.NotFound, result.Status);
        Assert.Null(result.Record);
        Assert.Equal("91330200SYNTHETIC", landDemandRepository.LastFindCreditCode);
    }

    [Fact]
    public async Task GetAsync_queries_only_by_the_authenticated_credit_code_and_returns_a_whitelisted_response()
    {
        var enterpriseRepository = new StubEnterpriseRepository(CreateEnterpriseProfile());
        var landDemandRepository = new StubLandDemandRepository
        {
            CurrentRecord = CreateStoredRecord()
        };
        var service = CreateService(enterpriseRepository, landDemandRepository);

        var result = await service.GetAsync(
            new EnterpriseIdentity(7, "91330200SYNTHETIC"),
            CancellationToken.None);

        Assert.Equal(LandDemandOperationStatus.Success, result.Status);
        Assert.NotNull(result.Record);
        Assert.Equal("91330200SYNTHETIC", landDemandRepository.LastFindCreditCode);
        Assert.Equal("Synthetic Enterprise", result.Record!.Businessname);
        Assert.Equal("91330200SYNTHETIC", result.Record.Creditcode);
        Assert.Equal("Yinzhou", result.Record.County);
        Assert.Equal("Shounan", result.Record.Region);
        Assert.Equal("1", result.Record.Landusedemand);
        Assert.Equal("2026-08-06 10:20:30", result.Record.Updatetime);
    }

    [Fact]
    public async Task AddAsync_returns_exists_without_calling_insert_when_the_authenticated_credit_code_already_has_a_record()
    {
        var enterpriseRepository = new StubEnterpriseRepository(CreateEnterpriseProfile());
        var landDemandRepository = new StubLandDemandRepository
        {
            CurrentRecord = CreateStoredRecord()
        };
        var service = CreateService(enterpriseRepository, landDemandRepository);

        var result = await service.AddAsync(
            new EnterpriseIdentity(7, "91330200SYNTHETIC"),
            ValidDraftRequest(),
            CancellationToken.None);

        Assert.Equal(LandDemandOperationStatus.Exists, result.Status);
        Assert.Null(result.Record);
        Assert.Equal(0, landDemandRepository.InsertCalls);
    }

    [Fact]
    public async Task AddAsync_returns_invalid_request_before_insert_when_the_payload_is_invalid()
    {
        var enterpriseRepository = new StubEnterpriseRepository(CreateEnterpriseProfile());
        var landDemandRepository = new StubLandDemandRepository();
        var service = CreateService(enterpriseRepository, landDemandRepository);

        var result = await service.AddAsync(
            new EnterpriseIdentity(7, "91330200SYNTHETIC"),
            new LandDemandWriteRequest { Landusedemand = "3" },
            CancellationToken.None);

        Assert.Equal(LandDemandOperationStatus.InvalidRequest, result.Status);
        Assert.Null(result.Record);
        Assert.Equal(0, landDemandRepository.InsertCalls);
    }

    [Fact]
    public async Task AddAsync_populates_identity_and_audit_fields_from_the_authenticated_enterprise_profile_and_clock()
    {
        var enterpriseRepository = new StubEnterpriseRepository(CreateEnterpriseProfile());
        var landDemandRepository = new StubLandDemandRepository();
        var service = CreateService(
            enterpriseRepository,
            landDemandRepository,
            () => new DateTimeOffset(2026, 8, 6, 10, 20, 30, TimeSpan.FromHours(8)));

        var result = await service.AddAsync(
            new EnterpriseIdentity(7, "91330200SYNTHETIC"),
            ValidSubmittedRequest(),
            CancellationToken.None);

        Assert.Equal(LandDemandOperationStatus.Success, result.Status);
        Assert.NotNull(result.Record);
        Assert.Equal(1, landDemandRepository.InsertCalls);
        Assert.NotNull(landDemandRepository.LastInsertedRecord);
        Assert.Equal("Synthetic Enterprise", landDemandRepository.LastInsertedRecord!.Businessname);
        Assert.Equal("91330200SYNTHETIC", landDemandRepository.LastInsertedRecord.Creditcode);
        Assert.Equal("Yinzhou", landDemandRepository.LastInsertedRecord.County);
        Assert.Equal("Shounan", landDemandRepository.LastInsertedRecord.Region);
        Assert.Equal("91330200SYNTHETIC", landDemandRepository.LastInsertedRecord.Updateuser);
        Assert.Equal("2026-08-06 10:20:30", landDemandRepository.LastInsertedRecord.Updatetime);
        Assert.Equal("2026-08-06 10:20:30", result.Record!.Updatetime);
    }

    [Fact]
    public async Task AddAsync_returns_exists_when_insert_throws_a_wrapped_duplicate_key_exception()
    {
        var enterpriseRepository = new StubEnterpriseRepository(CreateEnterpriseProfile());
        var landDemandRepository = new StubLandDemandRepository
        {
            InsertException = new InvalidOperationException(
                "wrapped duplicate",
                CreateMySqlDuplicateKeyException())
        };
        var service = CreateService(enterpriseRepository, landDemandRepository);

        var result = await service.AddAsync(
            new EnterpriseIdentity(7, "91330200SYNTHETIC"),
            ValidDraftRequest() with { Landusedemand = " 2 ", Projectdata = "  Save draft.  " },
            CancellationToken.None);

        Assert.Equal(LandDemandOperationStatus.Exists, result.Status);
        Assert.Null(result.Record);
        Assert.Equal(1, landDemandRepository.InsertCalls);
    }

    [Fact]
    public async Task UpdateAsync_returns_not_found_when_the_authenticated_credit_code_has_no_existing_record()
    {
        var enterpriseRepository = new StubEnterpriseRepository(CreateEnterpriseProfile());
        var landDemandRepository = new StubLandDemandRepository();
        var service = CreateService(enterpriseRepository, landDemandRepository);

        var result = await service.UpdateAsync(
            new EnterpriseIdentity(7, "91330200SYNTHETIC"),
            ValidDraftRequest(),
            CancellationToken.None);

        Assert.Equal(LandDemandOperationStatus.NotFound, result.Status);
        Assert.Null(result.Record);
        Assert.Equal(0, landDemandRepository.UpdateCalls);
    }

    [Fact]
    public async Task UpdateAsync_returns_invalid_request_without_updating_when_the_payload_is_invalid()
    {
        var enterpriseRepository = new StubEnterpriseRepository(CreateEnterpriseProfile());
        var landDemandRepository = new StubLandDemandRepository
        {
            CurrentRecord = CreateStoredRecord()
        };
        var service = CreateService(enterpriseRepository, landDemandRepository);

        var result = await service.UpdateAsync(
            new EnterpriseIdentity(7, "91330200SYNTHETIC"),
            ValidSubmittedRequest() with { FinancingTime = "2026-13", IsFinancing = "1" },
            CancellationToken.None);

        Assert.Equal(LandDemandOperationStatus.InvalidRequest, result.Status);
        Assert.Null(result.Record);
        Assert.Equal(0, landDemandRepository.UpdateCalls);
    }

    [Fact]
    public async Task UpdateAsync_updates_only_writable_fields_and_audit_values()
    {
        var enterpriseRepository = new StubEnterpriseRepository(CreateEnterpriseProfile());
        var landDemandRepository = new StubLandDemandRepository
        {
            CurrentRecord = CreateStoredRecord()
        };
        var service = CreateService(
            enterpriseRepository,
            landDemandRepository,
            () => new DateTimeOffset(2026, 8, 7, 9, 8, 7, TimeSpan.FromHours(8)));

        var request = ValidSubmittedRequest() with
        {
            Area = "80亩",
            Projectdata = "Expanded project description.",
            Landusedemand = "2"
        };

        var result = await service.UpdateAsync(
            new EnterpriseIdentity(7, "91330200SYNTHETIC"),
            request,
            CancellationToken.None);

        Assert.Equal(LandDemandOperationStatus.Success, result.Status);
        Assert.NotNull(result.Record);
        Assert.Equal(1, landDemandRepository.UpdateCalls);
        Assert.Equal("91330200SYNTHETIC", landDemandRepository.LastUpdateCreditCode);
        Assert.Equal("Synthetic Enterprise", result.Record!.Businessname);
        Assert.Equal("91330200SYNTHETIC", result.Record.Creditcode);
        Assert.Equal("Yinzhou", result.Record.County);
        Assert.Equal("Shounan", result.Record.Region);
        Assert.Equal("80亩", result.Record.Area);
        Assert.Equal("Expanded project description.", result.Record.Projectdata);
        Assert.Equal("2", result.Record.Landusedemand);
        Assert.Equal("2026-08-07 09:08:07", result.Record.Updatetime);
        Assert.NotNull(landDemandRepository.CurrentRecord);
        Assert.Equal(99, landDemandRepository.CurrentRecord!.Id);
        Assert.Equal("Synthetic Enterprise", landDemandRepository.CurrentRecord.Businessname);
        Assert.Equal("91330200SYNTHETIC", landDemandRepository.CurrentRecord.Creditcode);
        Assert.Equal("Yinzhou", landDemandRepository.CurrentRecord.County);
        Assert.Equal("Shounan", landDemandRepository.CurrentRecord.Region);
        Assert.Equal("2026-08-07 09:08:07", landDemandRepository.CurrentRecord.Updatetime);
        Assert.Equal("91330200SYNTHETIC", landDemandRepository.CurrentRecord.Updateuser);
    }

    [Fact]
    public async Task UpdateAsync_normalizes_writable_string_values_before_repository_update_and_response_mapping()
    {
        var enterpriseRepository = new StubEnterpriseRepository(CreateEnterpriseProfile());
        var landDemandRepository = new StubLandDemandRepository
        {
            CurrentRecord = CreateStoredRecord()
        };
        var service = CreateService(
            enterpriseRepository,
            landDemandRepository,
            () => new DateTimeOffset(2026, 8, 7, 9, 8, 7, TimeSpan.FromHours(8)));

        var request = ValidSubmittedRequest() with
        {
            Area = " 80 ",
            ExpectPark = "  Ningbo Industrial Park  ",
            Projectdata = "  Expanded project description.  ",
            Landusedemand = " 2 "
        };

        var result = await service.UpdateAsync(
            new EnterpriseIdentity(7, "91330200SYNTHETIC"),
            request,
            CancellationToken.None);

        Assert.Equal(LandDemandOperationStatus.Success, result.Status);
        Assert.NotNull(landDemandRepository.LastUpdateRequest);
        Assert.Equal("80", landDemandRepository.LastUpdateRequest!.Area);
        Assert.Equal("Ningbo Industrial Park", landDemandRepository.LastUpdateRequest.ExpectPark);
        Assert.Equal("Expanded project description.", landDemandRepository.LastUpdateRequest.Projectdata);
        Assert.Equal("2", landDemandRepository.LastUpdateRequest.Landusedemand);
        Assert.Equal("80", result.Record!.Area);
        Assert.Equal("Expanded project description.", result.Record.Projectdata);
        Assert.Equal("2", result.Record.Landusedemand);
        Assert.Equal("80", landDemandRepository.CurrentRecord!.Area);
        Assert.Equal("Expanded project description.", landDemandRepository.CurrentRecord.Projectdata);
        Assert.Equal("2", landDemandRepository.CurrentRecord.Landusedemand);
    }

    private static LandDemandService CreateService(
        StubEnterpriseRepository enterpriseRepository,
        StubLandDemandRepository landDemandRepository,
        Func<DateTimeOffset>? clock = null) =>
        new(
            new EnterpriseService(enterpriseRepository),
            landDemandRepository,
            clock ?? (() => new DateTimeOffset(2026, 8, 6, 10, 20, 30, TimeSpan.FromHours(8))));

    private static EnterpriseProfile CreateEnterpriseProfile() =>
        new()
        {
            UserId = 42,
            CreditCode = "91330200SYNTHETIC",
            BusinessName = "Synthetic Enterprise",
            CountyName = "Yinzhou",
            Region = "Shounan"
        };

    private static LandDemandRecord CreateStoredRecord() =>
        new()
        {
            Id = 99,
            Businessname = "Synthetic Enterprise",
            Creditcode = "91330200SYNTHETIC",
            County = "Yinzhou",
            Region = "Shounan",
            Area = "50亩",
            BuildingArea = 1200.50m,
            ExpectPark = "Ningbo Industrial Park",
            ExpectTime = "2026-08",
            IsDeploy = "0",
            DeployPark = null,
            IsSpecialuse = "0",
            DeployLandtype = null,
            DeployHeight = 12.5m,
            DeployWeight = 2.5m,
            Investment = 6000m,
            ProjectHydm = "A0111",
            Keyindustry = "高端装备",
            Futureindustry = "智能制造",
            PredYs = 7000m,
            PredTax = 800m,
            PredRdex = 300m,
            PredUnitenergy = 15m,
            Projectdata = "Build a new production line.",
            IsFinancing = "0",
            FinancingMoney = null,
            FinancingTime = null,
            Contact = "Alice",
            Office = "General Manager",
            Phone = "13800000000",
            Landusedemand = "1",
            Updatetime = "2026-08-06 10:20:30",
            Updateuser = "91330200SYNTHETIC"
        };

    private static LandDemandWriteRequest ValidDraftRequest() =>
        new()
        {
            Projectdata = "Save draft.",
            Landusedemand = "2"
        };

    private static LandDemandWriteRequest ValidSubmittedRequest() =>
        new()
        {
            Area = "50亩",
            BuildingArea = 1200.50m,
            ExpectPark = "Ningbo Industrial Park",
            ExpectTime = "2026-08",
            IsDeploy = "0",
            DeployPark = null,
            IsSpecialuse = "0",
            DeployLandtype = null,
            DeployHeight = 12.5m,
            DeployWeight = 2.5m,
            Investment = 6000m,
            ProjectHydm = "A0111",
            Keyindustry = "高端装备",
            Futureindustry = "智能制造",
            PredYs = 7000m,
            PredTax = 800m,
            PredRdex = 300m,
            PredUnitenergy = 15m,
            Projectdata = "Build a new production line.",
            IsFinancing = "0",
            FinancingMoney = null,
            FinancingTime = null,
            Contact = "Alice",
            Office = "General Manager",
            Phone = "13800000000",
            Landusedemand = "1"
        };

    private sealed class StubEnterpriseRepository : IEnterpriseRepository
    {
        private readonly EnterpriseProfile? profile;

        public StubEnterpriseRepository(EnterpriseProfile? profile)
        {
            this.profile = profile;
        }

        public string? LastCreditCode { get; private set; }

        public Task<EnterpriseProfile?> FindByCreditCodeAsync(string creditCode, CancellationToken cancellationToken)
        {
            LastCreditCode = creditCode;
            return Task.FromResult(profile is not null && profile.CreditCode == creditCode ? profile : null);
        }
    }

    private sealed class StubLandDemandRepository : ILandDemandRepository
    {
        public LandDemandRecord? CurrentRecord { get; set; }

        public string? LastFindCreditCode { get; private set; }

        public string? LastUpdateCreditCode { get; private set; }

        public LandDemandWriteRequest? LastUpdateRequest { get; private set; }

        public string? LastUpdateTime { get; private set; }

        public string? LastUpdateUser { get; private set; }

        public int InsertCalls { get; private set; }

        public int UpdateCalls { get; private set; }

        public LandDemandRecord? LastInsertedRecord { get; private set; }

        public Exception? InsertException { get; set; }

        public Task<LandDemandRecord?> FindByCreditCodeAsync(string creditCode, CancellationToken cancellationToken)
        {
            LastFindCreditCode = creditCode;
            return Task.FromResult(CurrentRecord is not null && CurrentRecord.Creditcode == creditCode ? Clone(CurrentRecord) : null);
        }

        public Task<LandDemandRecord> InsertAsync(LandDemandRecord record, CancellationToken cancellationToken)
        {
            InsertCalls++;
            if (InsertException is not null)
            {
                throw InsertException;
            }

            LastInsertedRecord = Clone(record);
            CurrentRecord = Clone(record);
            CurrentRecord.Id = record.Id == 0 ? 101 : record.Id;
            return Task.FromResult(Clone(CurrentRecord));
        }

        public Task<bool> UpdateWritableFieldsAsync(
            string creditCode,
            LandDemandWriteRequest request,
            string updateTime,
            string updateUser,
            CancellationToken cancellationToken)
        {
            LastUpdateCreditCode = creditCode;
            LastUpdateRequest = request;
            LastUpdateTime = updateTime;
            LastUpdateUser = updateUser;
            if (CurrentRecord is null || !string.Equals(CurrentRecord.Creditcode, creditCode, StringComparison.Ordinal))
            {
                return Task.FromResult(false);
            }

            UpdateCalls++;
            CurrentRecord.Area = request.Area;
            CurrentRecord.BuildingArea = request.BuildingArea;
            CurrentRecord.ExpectPark = request.ExpectPark;
            CurrentRecord.ExpectTime = request.ExpectTime;
            CurrentRecord.IsDeploy = request.IsDeploy;
            CurrentRecord.DeployPark = request.DeployPark;
            CurrentRecord.IsSpecialuse = request.IsSpecialuse;
            CurrentRecord.DeployLandtype = request.DeployLandtype;
            CurrentRecord.DeployHeight = request.DeployHeight;
            CurrentRecord.DeployWeight = request.DeployWeight;
            CurrentRecord.Investment = request.Investment;
            CurrentRecord.ProjectHydm = request.ProjectHydm;
            CurrentRecord.Keyindustry = request.Keyindustry;
            CurrentRecord.Futureindustry = request.Futureindustry;
            CurrentRecord.PredYs = request.PredYs;
            CurrentRecord.PredTax = request.PredTax;
            CurrentRecord.PredRdex = request.PredRdex;
            CurrentRecord.PredUnitenergy = request.PredUnitenergy;
            CurrentRecord.Projectdata = request.Projectdata;
            CurrentRecord.IsFinancing = request.IsFinancing;
            CurrentRecord.FinancingMoney = request.FinancingMoney;
            CurrentRecord.FinancingTime = request.FinancingTime;
            CurrentRecord.Contact = request.Contact;
            CurrentRecord.Office = request.Office;
            CurrentRecord.Phone = request.Phone;
            CurrentRecord.Landusedemand = request.Landusedemand;
            CurrentRecord.Updatetime = updateTime;
            CurrentRecord.Updateuser = updateUser;
            return Task.FromResult(true);
        }

        private static LandDemandRecord Clone(LandDemandRecord record) =>
            new()
            {
                Id = record.Id,
                County = record.County,
                Region = record.Region,
                Businessname = record.Businessname,
                Creditcode = record.Creditcode,
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
                Updatetime = record.Updatetime,
                Updateuser = record.Updateuser
            };
    }

    private static Exception CreateMySqlDuplicateKeyException()
    {
        var constructor = typeof(MySqlException).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            new[] { typeof(string), typeof(int) },
            modifiers: null);
        Assert.NotNull(constructor);
        return (Exception)constructor!.Invoke(new object[] { "Duplicate entry", 1062 });
    }
}
