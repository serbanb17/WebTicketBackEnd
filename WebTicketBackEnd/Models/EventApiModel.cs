using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebTicketBackEnd.Models
{
    public class EventApiModel
    {
        public String Id { get; set; }
        public String Name { get; set; }
        public String StartDate { get; set; }
        public String EndDate { get; set; }
        public String Location { get; set; }
        public String Description { get; set; }
        public String Url { get; set; }
        public String Image { get; set; }
    }
}
