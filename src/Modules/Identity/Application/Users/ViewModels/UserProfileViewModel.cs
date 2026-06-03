using System;
using System.Collections.Generic;
using LabViroMol.Modules.Identity.Contracts;

namespace LabViroMol.Modules.Identity.Application.Users.ViewModels;

public record UserProfileViewModel(Guid Id, UserInfo UserData, bool IsActive, List<string> Roles);
