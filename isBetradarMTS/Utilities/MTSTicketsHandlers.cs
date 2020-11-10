using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using isBetradarMTS.Domain;
using isBetradarMTS.Domain.Enumerations;
using log4net;
using Sportradar.MTS.SDK.API;
using Sportradar.MTS.SDK.Entities.Builders;
using Sportradar.MTS.SDK.Entities.Enums;
using Sportradar.MTS.SDK.Entities.EventArguments;
using Sportradar.MTS.SDK.Entities.Interfaces;

namespace isBetradarMTS.Utilities
{
    public class MTSTicketsHandlers
    {
        private readonly ILog _log;
        private IMtsSdk _mtsSdk;
        private IBuilderFactory _factory;
        private TicketGenerator _ticketGenerator;
        private readonly TicketService _ticketService;
        private ITicket mtsTicket;

        public MTSTicketsHandlers(IMtsSdk mtsSdk, IBuilderFactory builderFactory, ILog log)
        {
            _mtsSdk = mtsSdk;
            _factory = builderFactory;
            _log = log;
            _ticketService = new TicketService(_log);
            _ticketGenerator = new TicketGenerator(_factory, _log);
        }

        #region Tickets Sending Section       
        public void SendNewTickets(List<Ticket> tickets)
        {
            foreach(var ticket in tickets)
            {
                try 
                {
                    //ITicket mtsTicket = _ticketGenerator.GenerateNewTicket(ticket);
                    mtsTicket = _ticketGenerator.GenerateNewTicket(ticket);
                    _mtsSdk.SendTicket(mtsTicket);
                    //int stat = (int)BetStatus.s;
                    //int stat = (int)TicketStatus.waiting;
                    //_ticketService.UpdateTicketStatus(ticket.ticketId, stat.ToString());
                    _log.Info($"The ticket : {mtsTicket.TicketId} sent successfully.");
                    _log.Info($"The ticket JSon: { mtsTicket.ToJson() }");
                }
                catch (Exception ex)
                {
                    _log.Error($"Error in sending ticket, dbTicketId = {ticket.ticketId}", ex);
                }
            }
        }
        #endregion

        #region Ticket Response Handlers Section
        public Task HandleTicketResponse(ITicketResponse ticket)
        {
            Task task = null;
            _log.Info($"Ticket '{ticket.TicketId}' response is {ticket.Status}. Reason={ticket.Reason?.Message}");

            if (ticket.BetDetails != null && ticket.BetDetails.Any())
            {
                foreach (var betDetail in ticket.BetDetails)
                {
                    _log.Info($"Bet decline reason: '{betDetail.Reason?.Message}'.");
                    if (betDetail.SelectionDetails != null && betDetail.SelectionDetails.Any())
                    {
                        foreach (var selectionDetail in betDetail.SelectionDetails)
                        {
                            _log.Info($"Selection decline reason: '{selectionDetail.Reason?.Message}'.");
                        }
                    }
                }

                var tkId = Convert.ToInt64(ticket.TicketId.Split('_')[1]);
                if (ticket.Status == TicketAcceptance.Accepted)
                {
                    //required only if 'explicit acking' is enabled in MTS admin
                    //ticket.Acknowledge();
                    //update ticket status in db                
                    task = Task.Run(() =>
                    {
                        //int stat = (int)BetStatus.r;
                        int stat = (int)TicketStatus.running;
                        _ticketService.AcceptTicket(tkId, stat.ToString());
                    });
                    return task;
                }
                else if (ticket.Status == TicketAcceptance.Rejected)
                {
                    //REOFFER
                    //// if the ticket was declined and response has reoffer, the reoffer or reoffer cancellation can be send
                    //// the reoffer or reoffer cancellation must be send before predefined timeout, or is automatically canceled
                    if (ticket.BetDetails.Any(a => a.Reoffer != null))
                    {
                        if (ReofferShouldBeAccepted(ticket.Reason.Code))
                        {
                            // ReSharper disable once RedundantArgumentDefaultValue
                            var reofferTicket = _factory.CreateTicketReofferBuilder().Set(mtsTicket, ticket, null).BuildTicket();
                            _mtsSdk.SendTicket(reofferTicket);
                            //
                        }
                        else
                        {
                            var reofferCancel = _factory.CreateTicketReofferCancelBuilder().SetTicketId(ticket.TicketId).BuildTicket();
                            _mtsSdk.SendTicket(reofferCancel);
                            //update ticket status in db
                            task = Task.Run(() =>
                            {
                                //int stat = (int)BetStatus.d;
                                int stat = (int)TicketStatus.refused;
                                //_ticketService.UpdateTicketStatus(tkId, stat.ToString());
                                _ticketService.RejectTicket(tkId, stat.ToString(), ticket.Reason.Code);
                            });
                            return task;
                        }
                    }
                    else
                    {
                        if (ticket.Reason.Code == 101 || ticket.Reason.Code == 103 || ticket.Reason.Code == -101 || ticket.Reason.Code == -103)
                        {
                            ticket.Acknowledge();
                        }
                        //update ticket status in db
                        task = Task.Run(() =>
                        {
                            //int stat = (int)BetStatus.d;
                            int stat = (int)TicketStatus.refused;
                            //_ticketService.UpdateTicketStatus(tkId, stat.ToString());
                            _ticketService.RejectTicket(tkId, stat.ToString(), ticket.Reason.Code);
                        });
                        return task;
                    }
                }
            }
            return task;
        }
        public Task HandleTicketCancelResponse(ITicketCancelResponse ticket)
        {
            Task task = null;
            _log.Info($"Ticket '{ticket.TicketId}' response is {ticket.Status}. Reason={ticket.Reason?.Message}");
            if (ticket.Reason.Code == 101 || ticket.Reason.Code == 102 || ticket.Reason.Code == 103 || ticket.Reason.Code == -101 || ticket.Reason.Code == -102 || ticket.Reason.Code == -103)
            {
                ticket.Acknowledge();
            }
            
            if (ticket.Status == TicketCancelAcceptance.Cancelled)
            {
                //required only if 'explicit acking' is enabled in MTS admin
                //ticket.Acknowledge();
                task = Task.Run(() =>
                {
                    var tkId = Convert.ToInt64(ticket.TicketId.Split('_')[1]);
                    //int stat = (int)BetStatus.r;
                    int stat = (int)TicketStatus.cancelled;
                    _ticketService.UpdateTicketStatus(tkId, stat.ToString(), ticket.Reason.Code);
                });
                //Task.Run(() =>
                //{
                //    var tkId = Convert.ToInt64(ticket.TicketId.Split('_')[1]);
                //    int stat = (int)TicketStatus.cancelled;
                //    _ticketService.RejectTicket(tkId, stat.ToString());
                //});
            }
            else
            {
                task = Task.Run(() =>
                {
                    var tkId = Convert.ToInt64(ticket.TicketId.Split('_')[1]);
                    int stat = (int)TicketStatus.running;
                    _ticketService.UpdateTicketStatus(tkId, stat.ToString(), ticket.Reason.Code);
                });
            }
            return task;
        }
        public Task HandleTicketCashoutResponse(ITicketCashoutResponse ticketCashoutResponse)
        {
            Task task = null;
            _log.Info($"Ticket '{ticketCashoutResponse.TicketId}' response is {ticketCashoutResponse.Status}. Reason={ticketCashoutResponse.Reason?.Message}");
            ticketCashoutResponse.Acknowledge();
            if (ticketCashoutResponse.Status == CashoutAcceptance.Accepted)
            {
                //ticketCashoutResponse.Acknowledge();
                task = Task.Run(() =>
                {
                    var tkId = Convert.ToInt64(ticketCashoutResponse.TicketId.Split('_')[1]);
                    //int stat = (int)BetStatus.r;
                    int stat = (int)TicketStatus.win;
                    _ticketService.UpdateTicketStatus(tkId, stat.ToString(), ticketCashoutResponse.Reason.Code);
                });
            }
            else
            {
                task = Task.Run(() =>
                {
                    var tkId = Convert.ToInt64(ticketCashoutResponse.TicketId.Split('_')[1]);
                    int stat = (int)TicketStatus.refused;
                    _ticketService.RejectTicket(tkId, stat.ToString(), ticketCashoutResponse.Reason.Code);
                });
            }
            return task;
        }
        public Task HandleFailureTimeout(string ticketId)
        {
            var task = Task.Run(() =>
            {
                var tkId = Convert.ToInt64(ticketId.Split('_')[1]);
                //int stat = 9;
                int stat = (int)TicketStatus.waiting;
                _ticketService.UpdateTicketStatus(tkId, stat.ToString(), 0);
            });
            return task;
        }
        #endregion
        public bool ReofferShouldBeAccepted(int reasonCode)
        {
            switch (reasonCode)
            {
                case -701:
                    return true;
                case -702:
                    return true;
                case -703:
                    return true;
                case -711:
                    return true;
                case -712:
                    return true;
                case -713:
                    return true;
                case -721:
                    return true;
                case -722:
                    return true;
                case -723:
                    return true;
            }
            return false;
        }
    }
}
