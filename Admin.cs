using System.ComponentModel.DataAnnotations;

namespace CMCS
{
    public partial class Admin
    {
        [Key]
        public int AdminId { get; set; }
        public string AdminName { get; set; }
        public string Password { get; set; }


    }
}