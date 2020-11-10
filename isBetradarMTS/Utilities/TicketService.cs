using isBetradarMTS.Domain;
using isBetradarMTS.Domain.Enumerations;
using isBetradarUOF.DataAccess;
using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isBetradarMTS.Utilities
{
    public class TicketService
    {
        private ILog _log;
        private static ConcurrentDictionary<long, Ticket> storedTickets = new ConcurrentDictionary<long, Ticket>();
        private DateTime lastUpdateDate = new DateTime(1,1,1,1,1,1);
        public TicketService(ILog log)
        {
            _log = log;
        }

        public List<Ticket> GetUpdatedTickets()
        {
            List<Ticket> listTickets = null;
            List<Selection> betSelections = null;
            List<SelectedSystem> selectedSystems = null;
            using (var uow = new UnitOfWork(Program._connection, _log))
            {
                //getting all tickets from db
                listTickets = uow.TicketRepository.GetAll(lastUpdateDate);
                lastUpdateDate = DateTime.UtcNow;
                if (listTickets != null && listTickets.Count > 0)
                {
                    //geneating comma seperated ticketIds  
                    var ticketIds = listTickets.Select(t => t.ticketId);
                    var commaSeperatedTicketIds = string.Join<long>(",", ticketIds);

                    //updating tickets to gotten
                    //long updatedRows = -1;
                    ////int stat = (int)BetStatus.g;
                    //int stat = (int)TicketStatus.fetched;

                    //FOR TESTING
                    //updatedRows = uow.TicketRepository.BulkUpdateTicketStatus(commaSeperatedTicketIds, stat.ToString());

                    ////foreach (var tk in listTickets)
                    ////{
                    ////    Ticket newTicket;
                    ////    if (!storedTickets.TryGetValue(tk.ticketId, out newTicket))
                    ////    {
                    ////        newTicket = new Ticket();
                    ////        newTicket.ticketId = tk.ticketId;
                    ////        newTicket.currencyCode = tk.currencyCode;
                    ////        newTicket.customerClientId = tk.customerClientId;
                    ////        newTicket.betStack = tk.betStack;
                    ////        newTicket.bonusPlay = tk.bonusPlay;
                    ////        newTicket.providerId = tk.providerId;
                    ////        storedTickets.TryAdd(tk.ticketId, newTicket);
                    ////        _log.Info("Ticket: " + tk.ticketId + " has been fetched.");
                    ////        updatedRows += 1;
                    ////    }                        
                    ////}

                    _log.Info($"The selected tickets are: {commaSeperatedTicketIds} ");

                    //if (updatedRows == listTickets.Count)
                    //{
                    //    //getting all bet selections for the tickets
                    //    betSelections = uow.TicketRepository.GetAllBetSelections(commaSeperatedTicketIds);
                    //    selectedSystems = uow.TicketRepository.GetAllSelectedSystems(commaSeperatedTicketIds);
                    //}
                    //else
                    //{
                    //    _log.Error($"Updated rows: {updatedRows} are not equal Total No Tickets selected: {listTickets.Count}, setting null to tickets.");
                    //    _log.Info($"The selected tickets are: {commaSeperatedTicketIds} ");
                    //    listTickets = null;
                    //}

                    betSelections = uow.TicketRepository.GetAllBetSelections(commaSeperatedTicketIds);
                    selectedSystems = uow.TicketRepository.GetAllSelectedSystems(commaSeperatedTicketIds);

                    //FOR TESTING
                }
                uow.Commit();

                //FOR TESTING
                //store in dictionary for update in contabilita/bonus (just in case rejected, cancelled, etc.)
                if (listTickets != null && listTickets.Count > 0)
                {
                    foreach (Ticket tk in listTickets)
                    {
                        if (!storedTickets.TryGetValue(tk.ticketId, out _))
                        {
                            Ticket newTicket = new Ticket();
                            newTicket.ticketId = tk.ticketId;
                            newTicket.currencyCode = tk.currencyCode;
                            newTicket.customerClientId = tk.customerClientId;
                            newTicket.bonusPlay = tk.bonusPlay;
                            newTicket.betStack = tk.betStack;
                            newTicket.bonusPlay = tk.bonusPlay;
                            newTicket.providerId = tk.providerId;
                            newTicket.cashOutMoney = tk.cashOutMoney;
                            newTicket.betType = tk.betType;

                            if (tk.betType == (int)BetType.I || tk.betType == (int)BetType.C)
                            {
                                tk.betBonus = tk.bonusMin;
                            }
                            newTicket.betBonus = tk.betBonus;

                            newTicket.updateDate = lastUpdateDate; // tk.updateDate;
                            storedTickets.TryAdd(tk.ticketId, newTicket);

                            //if (tk.updateDate >= lastUpdateDate)
                            //{
                            //    lastUpdateDate = tk.updateDate;
                            //}
                        }
                    }
                }
                //FOR TESTING
            }
            if (listTickets != null && listTickets.Count > 0 && betSelections != null && betSelections.Count > 0)
            {
                foreach (var tk in listTickets)
                {
                    //taking selection to the ticket object
                    foreach (var sl in betSelections)
                    {
                        if (tk.ticketId == sl.ticketId)
                        {
                            tk.Selections.Add(sl);
                        }
                    }
                    // taking selected systems to the ticket
                    if(tk.betType == (int)BetType.C && selectedSystems != null)
                    {
                        foreach(var ss in selectedSystems)
                        {
                            if (tk.ticketId == ss.ticketId)
                            {
                                tk.selectedSystem.Add(ss.kind);
                            }
                        }
                    }
                    
                }
                return listTickets;
            }
            else
            {
                _log.Info($"No tickets available in db, and returning TicketsList = null");
            }          
            return null;
        }

        public void CheckSentTickets()
        {
            List<Task> result = new List<Task>();
            result.Add(Task.Run(() =>
            {
                foreach (var toUpdTicket in storedTickets)
                {
                    if (toUpdTicket.Value.updateDate != lastUpdateDate)
                    {
                        TimeSpan timeD = DateTime.UtcNow - toUpdTicket.Value.updateDate;
                        if (timeD.TotalSeconds > 20)
                        {
                            int stat = (int)TicketStatus.refused;
                            RejectTicket(toUpdTicket.Value.ticketId, stat.ToString(), 102);
                        }
                    }
                }
            }));
            Task.WaitAll(result.ToArray());
        }

        public bool AcceptCashoutTicket(long ticketId, string betStatus, int reserveReasonId)
        {
            Ticket cashOutTicket;
            if (storedTickets.TryGetValue(ticketId, out cashOutTicket))
            {
                using (var uow = new UnitOfWork(Program._connection, _log))
                {
                    if (cashOutTicket.bonusPlay == 0)
                    {
                        long resolvedTickets = uow.TicketRepository.ResolveCashout(cashOutTicket);
                    }
                    else
                    {
                        long resBoTickets = uow.TicketRepository.ResBonusCashout(cashOutTicket);
                    }                    
                    
                    long processedTickets = uow.TicketRepository.UpdateTicketStatus(ticketId, betStatus, reserveReasonId);
                    uow.Commit();
                }

                if (storedTickets.TryRemove(ticketId, out _))
                {
                    _log.Info("AcceptCashoutTicket Ticket: " + ticketId);
                }

                return true;                
            }
            return false;
        }

        public bool UpdateTicketStatus(long ticketId, string betStatus, int reserveReasonId)
        {
            long numberUpdated = -1;

            if (storedTickets.TryGetValue(ticketId, out _))
            {
                using (var uow = new UnitOfWork(Program._connection, _log))
                {
                    numberUpdated = uow.TicketRepository.UpdateTicketStatus(ticketId, betStatus, reserveReasonId);
                    uow.Commit();
                }
            }           

            //_log.Info("Ticket: " + ticketId.ToString() + " has been updated with status: " + betStatus);
            //numberUpdated = 1;
            //FOR TESTING

            if (numberUpdated > 0)
            {
                if (storedTickets.TryRemove(ticketId, out _))
                {
                    _log.Info("Updated Ticket: " + ticketId);
                }
                return true;
            }
            else
            {
                return false;
            }                
        }

        public bool AcceptTicket(long ticketId, string betStatus)
        {
            long numberUpdated = -1;

            if (storedTickets.TryGetValue(ticketId, out _))
            {
                using (var uow = new UnitOfWork(Program._connection, _log))
                {
                    numberUpdated = uow.TicketRepository.UpdAcceptTicket(ticketId, betStatus);
                    uow.Commit();
                }
            }

            //_log.Info("Ticket: " + ticketId.ToString() + " has been accepted with status: " + betStatus);
            //numberUpdated = 1;
            //FOR TESTING

            if (storedTickets.TryRemove(ticketId, out _))
            {
                _log.Info("AcceptTicket Ticket: " + ticketId);
            }

            if (numberUpdated > 0)
                return true;
            else
                return false;
        }

        public bool RejectTicket(long ticketId, string betStatus, int reserveReasonId)
        {
            Ticket reTicket;
            bool isBonusPlay = false;
            long resolvedTickets = 0;
            long numberUpdated = -1;

            if (storedTickets.TryGetValue(ticketId, out reTicket))
            {
                if (reTicket.bonusPlay == 1)
                {
                    isBonusPlay = true;
                }

                using (var uow = new UnitOfWork(Program._connection, _log))
                {
                    if (isBonusPlay)
                    {
                        resolvedTickets = uow.TicketRepository.ResoBonusRejected(reTicket);
                    }
                    else
                    {
                        resolvedTickets = uow.TicketRepository.ResolveRejected(reTicket);
                    }
                    numberUpdated = uow.TicketRepository.UpdateTicketStatus(ticketId, betStatus, reserveReasonId);
                    uow.Commit();
                }
            }   

            if (storedTickets.TryRemove(ticketId, out _))
            {
                _log.Info("RejectTicket Ticket: " + ticketId);
            }

            //if (isBonusPlay)
            //{
            //    _log.Info("Ticket: " + reTicket.ticketId.ToString() + " has been rejected (bonus play) with bet stack: " + reTicket.betStatus);
            //    numberUpdated = 1;
            //}
            //else
            //{
            //    _log.Info("Ticket: " + reTicket.ticketId.ToString() + " has been rejected with bet stack: " + reTicket.betStatus);
            //    numberUpdated = 1;
            //}
            _log.Info("Ticket: " + ticketId.ToString() + " has been updated with status: " + betStatus);
            //FOR TESTING

            if (numberUpdated > 0)
                return true;
            else
                return false;
        }
    }
}
