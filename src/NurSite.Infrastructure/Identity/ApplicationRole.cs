using Microsoft.AspNetCore.Identity;

namespace NurSite.Infrastructure.Identity;

public class ApplicationRole : IdentityRole
{
    public string? DisplayName { get; set; }
    public string? Description { get; set; }

    public ApplicationRole() { }
    public ApplicationRole(string name) : base(name) { }
}
