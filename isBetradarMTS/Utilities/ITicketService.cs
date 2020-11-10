using isBetradarMTS.Domain;
using isBetradarMTS.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isBetradarMTS.Utilities
{
    public interface ITicketService
    {
        List<Ticket> GetUpdatedTickets();
        //bool UpdateTicketStatus(long ticketId, BetStatus betStatus);
        bool UpdateTicketStatus(long ticketId, TicketStatus ticketStatus);
    }
}
