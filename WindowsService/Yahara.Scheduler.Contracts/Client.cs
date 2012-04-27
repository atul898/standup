using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Yahara.Scheduler.Contracts
{
    [DataContract]
    public class Client
    {
        [DataMember]
        public int ClientId { get; set; }

        [DataMember]
        public string ClientName { get; set; }
    }
}
