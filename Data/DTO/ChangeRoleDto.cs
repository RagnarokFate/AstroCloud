using AstroCloud.Data.Enum;
using System.ComponentModel.DataAnnotations;

namespace AstroCloud.Data.DTO
{
    public class ChangeRoleDto
    {
        [Required]
        public UserType NewRole { get; set; }
    }
}
