﻿using System.ComponentModel.DataAnnotations;

namespace CMCS
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }

        
    }
}