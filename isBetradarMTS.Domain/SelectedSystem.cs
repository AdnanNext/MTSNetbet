using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isBetradarMTS.Domain
{
    public class SelectedSystem
    {
        public long ticketId { get; set; }
        public long kind  { get; set; }
        public int col { get; set; }
        public double amount { get; set; }
    }
}
