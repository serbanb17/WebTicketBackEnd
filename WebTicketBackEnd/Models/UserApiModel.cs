﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebTicketBackEnd.Models
{
    public class UserApiModel
    {
        public String Email { get; set; }
        public String Password { get; set; }
        public String Birthday { get; set; }
        public String Name { get; set; }
        public String Surname { get; set; }
    }
}
