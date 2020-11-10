using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isBetradarMTS.Domain
{
    public class Selection
    {
        /// <summary>
        /// evento
        /// </summary>
        public long matchId { get; set; }
        /// <summary>
        /// not availble
        /// </summary>
        public string betId { get; set; }
        /// <summary>
        /// id_schedina
        /// </summary>
        public long ticketId { get; set; }
        /// <summary>
        /// live
        /// </summary>
        public int producerId { get; set; }
        public long sportId { get; set; }
        public long marketId { get; set; }
        public long specId { get; set; }
        /// <summary>
        /// should be speated with  &  like setnr=2&gamnr=2&pointnr=2
        /// </summary>
        public string specifiers { get; set; }
        public double odd { get; set; }
        public string spread { get; set; }
    }
}
