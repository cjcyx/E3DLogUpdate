using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3DLogUpdate
{
    public class ReceivedInfos
    {
        public int timeStamp { get; set; }
        public string module { get; set; }
        public int status { get; set; }
        public int currentUsage { get; set; }
        public string compName { get; set; }
        public string userName { get; set; }
        public string serverName { get; set; }
    }
}
