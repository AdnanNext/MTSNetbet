using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace isBetradarMTS.Domain
{
    public class Ticket
    {
        public Ticket()
        {
            Selections = new List<Selection>();
            selectedSystem = new List<long>();
        }
        /// <summary>
        /// db field: id
        /// </summary>
        public long ticketId { get; set; }
        /// <summary>
        /// db field: valuta
        /// </summary>
        public string currencyCode { get; set; }
        /// <summary>
        /// db field: id_login
        /// </summary>
        public string customerClientId { get; set; }
        /// <summary>
        /// optional skip it
        /// </summary>
        public int confidence { get; set; }
        /// <summary>
        /// send static 'en'
        /// </summary>
        public string languageCode { get; set; }
        //optional
        public string userDeviceId { get; set; }
        /// <summary>
        /// db field: ip_address
        /// </summary>
        public string userIPAddress { get; set; }
        //bet fields: bonus
        public double betBonus { get; set; }
        /// <summary>
        /// db field: puntata
        /// </summary>
        public int betStack { get; set; }
        /// <summary>
        /// use static: total
        /// </summary>
        public string stackType { get; set; }
        /// <summary>
        /// db field: status
        /// </summary>
        public string betStatus { get; set; }
        /// <summary>
        /// db field: tipo
        /// </summary>
        public int betType { get; set; }
        /// <summary>
        /// db field: numero_segni
        /// </summary>
        public long numberSelection { get; set; }
        /// <summary>
        /// db field: segni_singoli
        /// </summary>
        public long totalSelection { get; set; }
        //agrigated collection of selections in a bet
        public List<Selection> Selections { get; set; }
        /// <summary>
        /// calculated from selectinos
        /// </summary>
        public List<long> selectedSystem { get; set; }
        public int bonusPlay { get; set; }
        public long providerId { get; set; }
        public string rejectionCode { get; set; }
        public string operazione { get; set; }
        public long rejIP { get; set; }
        public DateTime updateDate { get; set; }
        public double cashOutMoney { get; set; }
        public double bonusMin { get; set; }
    }

}
