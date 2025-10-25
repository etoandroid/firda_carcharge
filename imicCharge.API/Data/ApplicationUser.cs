using Microsoft.AspNetCore.Identity;

namespace imicCharge.API.Data
{
    public class ApplicationUser : IdentityUser
    {
        public decimal AccountBalance { get; set; } = 0;
    }
}