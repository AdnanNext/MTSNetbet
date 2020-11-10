using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isBetradarMTS.Domain.Enumerations
{
    public enum TicketStatus
    {
        /// <summary>
        /// not yet approved
        /// </summary>
        no_status = 100,
        running = 1,
        win = 2,
        lost = 3,
        /// <summary>
        /// rejected by mts
        /// </summary>
        refused = 4,
        /// <summary>
        /// cancelled by mts
        /// </summary>
        cancelled = 5,
        cashout_accepted = 6,
        cashout_rejected = 7,
        /// <summary>
        /// taken from db to send
        /// </summary>
        fetched = 8,
        /// <summary>
        /// evaluation or sent to mts
        /// </summary>
        waiting = 9,
        promotion = 10,
        live_check = 11,
        partial_win = 12
    }
}
