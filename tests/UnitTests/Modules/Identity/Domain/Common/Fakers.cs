using Bogus;
using LabViroMol.Modules.Identity.Domain.Users;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Identity.Domain.UnitTests.Common;

internal static class Fakers
{
    private static readonly Faker F = new("pt_BR");

    public static UserId AnyUserId() => IdFactory.New<UserId>();

    public static UserName AnyUserName()
        => new(F.Name.FirstName(), F.Name.LastName());

    public static Email AnyEmail()
        => new(F.Internet.Email());

    public static User CreateUser(UserId? id = null)
        => User.Create(id ?? AnyUserId(), AnyUserName(), AnyEmail(), null, null);

    public static User CreateDeactivatedUser()
    {
        var user = CreateUser();
        user.Deactivate();
        return user;
    }
}
