using isBetradarMTS.Domain;
using isBetradarMTS.Domain.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isBetradarMTS.DataAccess.Repositories
{
    public interface ITicketRepository : IRepository<Ticket>
    {
        List<Ticket> GetAll(DateTime updateDate);
        List<Selection> GetAllBetSelections(string commaSeperatedTicketIds);
        List<SelectedSystem> GetAllSelectedSystems(string commaSeperatedTicketIds);
        //FOR TESTING
        long BulkUpdateTicketStatus(string commaSeperatedTicketIds, string ticketStatus);
        //long UpdateTicketStatus(long ticketId, string ticketStatus);
        long UpdateTicketStatus(long ticketId, string ticketStatus, int reserveReasonId);
        long UpdAcceptTicket(long ticketId, string ticketStatus);
        long ResolveRejected(Ticket ticket);
        long ResoBonusRejected(Ticket ticket);
        //FOR TESTING

        long ResolveCashout(Ticket cashoutTicket);
        long ResBonusCashout(Ticket cashoutTicket);
    }
}
