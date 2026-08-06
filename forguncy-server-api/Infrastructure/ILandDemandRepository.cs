using ForguncyServerApi.Application;
using ForguncyServerApi.Domain;

namespace ForguncyServerApi.Infrastructure;

public interface ILandDemandRepository
{
    Task<LandDemandRecord?> FindByCreditCodeAsync(string creditCode, CancellationToken cancellationToken);

    Task<LandDemandRecord> InsertAsync(LandDemandRecord record, CancellationToken cancellationToken);

    Task<bool> UpdateWritableFieldsAsync(
        string creditCode,
        LandDemandWriteRequest request,
        string updateTime,
        string updateUser,
        CancellationToken cancellationToken);
}
