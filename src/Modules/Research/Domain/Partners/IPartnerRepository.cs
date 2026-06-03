using System.Threading;
using System.Threading.Tasks;

namespace LabViroMol.Modules.Research.Domain.Partners;

public interface IPartnerRepository
{
    Task<Partner?> GetByIdAsync(PartnerId id, CancellationToken ct);
    Task AddAsync(Partner partner, CancellationToken ct);
    void Delete(Partner partner);
}
