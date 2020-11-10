using Dapper;
using isBetradarMTS.Domain;
using isBetradarMTS.Domain.Enumerations;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isBetradarMTS.DataAccess.Repositories
{
    public class TicketRepository : RepositoryBase, ITicketRepository
    {
        private readonly ILog _log;
        public TicketRepository(IDbTransaction transaction, ILog log) : base(transaction)
        {
            _log = log;
        }

        public long Add(Ticket entity)
        {
            throw new NotImplementedException();
        }

        public long BulkAdd(List<Ticket> listEntity)
        {
            throw new NotImplementedException();
        }

        public List<Ticket> GetAll(DateTime updateDate)
        {
            try
            {
                //string sql = @"SELECT id AS ticketId, currency as currencyCode, loginId AS customerClientId, ip_address AS userIPAddress, bonus AS betBonus, bonus_min AS bonusMin, providerId, updateDate,price AS cashOutMoney,
                //                   puntata AS betStack, status AS betStatus, tipo as betType, numero_segni as numberSelection, segni_singoli as totalSelection, bonus_play AS bonusPlay
                //                FROM ticket
                //                WHERE status IN (10, 5000) AND updateDate > '" + updateDate.ToString("yyyy-MM-dd HH:mm:ss") + "';"; //1 AND timestampdiff(second,updateDate,now()) < 10;";

                string sql = string.Format("SELECT id AS ticketId, currency as currencyCode, loginId AS customerClientId, ip_address AS userIPAddress, bonus AS betBonus, bonus_min AS bonusMin, providerId, updateDate,price AS cashOutMoney, puntata AS betStack, status AS betStatus, tipo as betType, numero_segni as numberSelection, segni_singoli as totalSelection, bonus_play AS bonusPlay ");
                string whereClause = string.Format(" FROM ticket WHERE status IN(10, 5000) AND updateDate > '{0}';", ((DateTime)updateDate).ToString("yyyy-MM-dd HH:mm:ss"));
                sql += whereClause;
                var queryResult = Connection.Query<Ticket>(sql, transaction: Transaction);
                if (queryResult != null)
                {
                    return queryResult.ToList();
                }
                return null;
            }
            catch (Exception ex)
            {
                _log.Error("Error in GetAll Tickets: " + ex.Message);
                return null;
            }
        }

        public List<Selection> GetAllBetSelections(string commaSeperatedTicketIds)
        {
            //getting bet selections
            var sqlSelections = string.Format(@"SELECT eventId AS matchId, ticketId, tipo AS producerId, sportId,
	                                                   marketId, specifiers, specId, quota AS odd, spread
                                                FROM ticketLine
                                                WHERE ticketId IN({0});", commaSeperatedTicketIds);
            var subQueryResult = Connection.Query<Selection>(sqlSelections, transaction: Transaction);
            if (subQueryResult != null)
            {
                return subQueryResult.ToList();
            }
            else
                return null;
        }
        public List<SelectedSystem> GetAllSelectedSystems(string commaSeperatedTicketIds)
        {
            //getting bet selections
            var sqlSelections = string.Format(@"SELECT ticketId, kind, col, amount 
                                                FROM ticketSystem
                                                WHERE ticketId IN({0});", commaSeperatedTicketIds);
            var subQueryResult = Connection.Query<SelectedSystem>(sqlSelections, transaction: Transaction);
            if (subQueryResult != null)
            {
                return subQueryResult.ToList();
            }
            else
                return null;
        }

        public long Update(Ticket entity)
        {
            throw new NotImplementedException();
        }
        //FOR TESTING
        public long BulkUpdateTicketStatus(string commaSeperatedTicketIds, string ticketStatus)
        {
            try
            {
                string sql = string.Format("UPDATE `ticket` SET `status` = '{0}' WHERE `id` in({1})", ticketStatus, commaSeperatedTicketIds);
                return Connection.Execute(sql, transaction: Transaction);
            }
            catch (Exception ex)
            {
                _log.Error($"Error in bulk updating ticket status = {ticketStatus} : ", ex);
                return -1;
            }
        }
        public long UpdateTicketStatus(long ticketId, string ticketStatus, int reserveReasonId)
        {
            try
            {
                //string sql = @"UPDATE `ticket`
                //                SET
                //                    `status` = @TicketStatus,
                //                    `reserveReasonId` = @ReserveReasonId
                //                WHERE `id` = @TicketId;";
                string sql = string.Format("UPDATE `ticket` SET `status` = '{0}', `reserveReasonId` = {1} WHERE `id` = {2};", ticketStatus, reserveReasonId, ticketId);
                return Connection.Execute(sql, transaction: Transaction);
            }
            catch (Exception ex)
            {
                _log.Error($"Error in updating single ticket status = {ticketStatus} : ", ex);
                return -1;
            }
        }
        public long ResolveCashout(Ticket cashoutTicket)
        {
            try
            {
                cashoutTicket.rejIP = 0;
                cashoutTicket.rejectionCode = "CSH";
                cashoutTicket.operazione = "+";
                string sql = string.Format("INSERT INTO contabilita (loginId, valuta, operazione, importo, tipo, operatoreId, codice, ip) Values({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});", cashoutTicket.customerClientId, cashoutTicket.currencyCode, cashoutTicket.operazione, cashoutTicket.betStack, cashoutTicket.rejectionCode, cashoutTicket.providerId, cashoutTicket.ticketId, cashoutTicket.rejIP);
                return Connection.Execute(sql, transaction: Transaction);
            }
            catch (Exception ex)
            {
                _log.Error($"Error in ResolveCashout: " + ex.Message);
                return -1;
            }
        }

        public long ResBonusCashout(Ticket cashoutTicket)
        {
            try
            {
                cashoutTicket.rejIP = 0;
                cashoutTicket.rejectionCode = "CSH";
                cashoutTicket.operazione = "+";
                string sql = string.Format("INSERT INTO contabilitaBonus (loginId, valuta, operazione, importo, tipo, operatoreId, codice, ip) Values({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});", cashoutTicket.customerClientId, cashoutTicket.currencyCode, cashoutTicket.operazione, cashoutTicket.betStack, cashoutTicket.rejectionCode, cashoutTicket.providerId, cashoutTicket.ticketId, cashoutTicket.rejIP);
                return Connection.Execute(sql, transaction: Transaction);
            }
            catch (Exception ex)
            {
                _log.Error($"Error in ResolveCashout: " + ex.Message);
                return -1;
            }
        }
        public long UpdAcceptTicket(long ticketId, string ticketStatus)
        {
            try
            {
                //string sql = @"UPDATE `ticket`
                //                SET
                //                    `status` = @ticketStatus,
                //                    `accettatore` = `providerID`,
                //                    `data_accettazione` = NOW()
                //                WHERE `id` = @ticketId;";
                string sql = string.Format("UPDATE `ticket` SET `status` = '{0}', `accettatore` = `providerID`, `data_accettazione` = NOW() WHERE `id` = {1};", ticketStatus, ticketId);
                return Connection.Execute(sql, transaction: Transaction);
            }
            catch (Exception ex)
            {
                _log.Error($"Error in updating single ticket status = {ticketStatus} : ", ex);
                return -1;
            }
        }
        public long ResolveRejected(Ticket ticket)
        {
            try
            {
                ticket.rejIP = 0;
                ticket.rejectionCode = "RCP";
                ticket.operazione = "+";
                //string sql = @"INSERT INTO contabilita (loginId, valuta, operazione, importo, tipo, operatoreId, codice, ip)
                //                Values(@LoginId, @Valuta, @Operazione, @Importo, @Tipo, @OperatoreId, @Codice, @Ip); ";
                string sql = string.Format("INSERT INTO contabilita (loginId, valuta, operazione, importo, tipo, operatoreId, codice, ip) Values({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7});", ticket.customerClientId, ticket.currencyCode, ticket.operazione, ticket.betStack, ticket.rejectionCode, ticket.providerId, ticket.ticketId, ticket.rejIP);
                return Connection.Execute(sql, transaction: Transaction);
            }
            catch (Exception ex)
            {
                _log.Error($"Error in resolving ticket: " + ex.Message + Environment.NewLine + " Stack Trace: " + ex.StackTrace);
                return -1;
            }
        }
        public long ResoBonusRejected(Ticket ticket)
        {
            try
            {
                ticket.rejIP = 0;
                ticket.rejectionCode = "RCP";
                ticket.operazione = "+";
                //string sql = @"INSERT INTO contabilitaBonus (loginId, valuta, operazione, importo, tipo, operatoreId, codice, ip)
                //                Values(@LoginId, @Valuta, @Operazione, @Importo, @Tipo, @OperatoreId, @Codice, @Ip); ";

                string sql = string.Format("INSERT INTO contabilitaBonus (loginId, valuta, operazione, importo, tipo, operatoreId, codice, ip) Values({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}); ", ticket.customerClientId, ticket.currencyCode, ticket.operazione, ticket.betStack, ticket.rejectionCode, ticket.providerId, ticket.ticketId, ticket.rejIP);
                return Connection.Execute(sql, transaction: Transaction);
            }
            catch (Exception ex)
            {
                _log.Error($"Error in resolving ticket (bonus): " + ex.Message + Environment.NewLine + " Stack Trace: " + ex.StackTrace);
                return -1;
            }
        }
        //FOR TESTING
    }
}
