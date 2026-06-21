namespace LabViroMol.Modules.Identity.Application.Roles.Queries;

public interface IPermissionQueries
{
    IReadOnlyCollection<string> GetAll();
}
