using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isBetradarMTS.Domain.Enumerations
{
    public enum BetStatus
    {
        /// <summary>
        /// not sent
        /// </summary>
        b,
        /// <summary>
        /// rejected by mts
        /// </summary>
        d,
        /// <summary>
        /// accepted by mts
        /// </summary>
        r,
        /// <summary>
        /// sent to mts
        /// </summary>
        s,
        /// <summary>
        /// taken from db to send
        /// </summary>
        g
    }
}
