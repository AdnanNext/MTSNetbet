using isBetradarMTS.Domain;
using isBetradarMTS.Domain.Enumerations;
using log4net;
using Sportradar.MTS.SDK.Entities.Builders;
using Sportradar.MTS.SDK.Entities.Enums;
using Sportradar.MTS.SDK.Entities.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isBetradarMTS.Utilities
{
    public class TicketGenerator
    {
        private readonly ILog _log;
        private IBuilderFactory _factory;
        private const int seed = 10000;
        private readonly string _languageCode;
        public TicketGenerator(IBuilderFactory factory, ILog log)
        {
            _factory = factory;
            _log = log;
            _languageCode = AppSettings.GetAppSettings("TicketLanguageCode");
        }

        public ITicket GenerateNewTicket(Ticket objTicket)
        {
            if (objTicket != null)
            {
                //ticket object creation
                var preTicket = _factory.CreateTicketBuilder()
                                     .SetTicketId("NetbetTicket_" + objTicket.ticketId)
                                     .SetSender(_factory.CreateSenderBuilder()
                                    .SetCurrency(objTicket.currencyCode)
                                    .SetEndCustomer(_factory.CreateEndCustomerBuilder()
                                        .SetId("NetbetUserId_" + objTicket.customerClientId)
                                        //.SetConfidence(10000)  //optional
                                        //.SetIp(IPAddress.Loopback) //optional
                                        .SetLanguageId(_languageCode)
                                        //.SetDeviceId("UsersDeviceId-") //optional
                                        .Build())
                                    .Build());

                //bets iteration
                var betBonus = Convert.ToInt64(objTicket.betBonus * seed);
                var betStack = objTicket.betStack * seed;
                if(objTicket.betType == (int)BetType.S || objTicket.betType == (int)BetType.M)
                {
                    //calculating selectedSystem for singles and multiples
                    objTicket.selectedSystem.Add(objTicket.Selections.Count);
                }
                else if(objTicket.betType == (int)BetType.I)
                {
                    //calculating selectedSystem for integrals
                    objTicket.selectedSystem.Add(objTicket.numberSelection);
                }

                //creating single bet object
                var preBet = _factory.CreateBetBuilder()
                                     .SetBetId("NetbetBetId_" + objTicket.ticketId)
                                     .SetStake(Convert.ToInt64(betStack), StakeType.Total);
                //bet bonus
                if(betBonus > 0)
                {
                    preBet.SetBetBonus(betBonus);
                }

                //setting selected System
                foreach(var selectedSystem in objTicket.selectedSystem)
                {
                    preBet.AddSelectedSystem((int)selectedSystem);
                }      

                //setting ticket selections
                foreach (var s in objTicket.Selections)
                {
                    //determining the producer
                    //var producer = 0;
                    //if (s.producerId == Producer.O.ToString())
                    //    producer = (int)Producer.O;
                    //else if (s.producerId == Producer.L.ToString())
                    //    producer = (int)Producer.L;
                    
                    if (s.producerId == 0) //is pre-match
                    {
                        s.producerId = (int)Producer.L;
                    }
                    else
                    {
                        s.producerId = (int)Producer.O;
                    }

                    var eventId = "sr:match:" + s.matchId.ToString();
                    var selectionId = string.Format("uof:{0}/sr:sport:{1}/{2}/{3}", s.producerId, s.sportId, s.marketId, s.specId);
                    if (s.specifiers != null && s.specifiers != "" && s.spread != null && s.spread != "")
                    {                        
                        if (s.specifiers.Contains('|'))
                        {
                            string[] specifierList = s.specifiers.Split('|');
                            int counter = 0;
                            foreach (var specif in specifierList)
                            {
                                //int len = specif.Length - 7;
                                if (counter == 0)
                                {
                                    selectionId += string.Format("?{0}={1}", specif/*.Substring(6, len)*/, s.spread);
                                }
                                else
                                {
                                    selectionId += string.Format("&{0}={1}", specif/*.Substring(6, len)*/, s.spread);
                                }
                                counter += 1;
                            }
                        }
                        else
                        {
                            //int len = s.specifiers.Length - 7;
                            selectionId += string.Format("?{0}={1}", s.specifiers/*.Substring(6, len)*/, s.spread);
                        }                        
                    }
                    var odds = Convert.ToInt32(s.odd * seed);

                    //creating single selection
                    var selection = _factory.CreateSelectionBuilder()
                                            .SetEventId(eventId)
                                            .SetId(selectionId)
                                            .SetOdds(odds)
                                            .Build();
                    //adding selection to the bet
                    preBet.AddSelection(selection);
                }

                //building the bet after adding all selections
                var bet = preBet.Build();

                //adding bet to the ticket object
                preTicket.AddBet(bet);

                //building the ticket after adding all bets
                var ticket = preTicket.BuildTicket();
                return ticket;
            }
            else
                return null;
        }
    }
}
