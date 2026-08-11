using SEAL_Domain.Base;
using System.Collections.Generic;

namespace SEAL_Domain.Entity
{
    public class School : BaseEntity
    {
        public string SchoolName { get; set; } = string.Empty;
        public string? Address { get; set; }

        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
