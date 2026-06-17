using LabViroMol.Modules.Shared.Kernel.Primitives;
using Microsoft.AspNetCore.Identity;

namespace LabViroMol.Modules.Identity.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>, IDeletionAuditable;
