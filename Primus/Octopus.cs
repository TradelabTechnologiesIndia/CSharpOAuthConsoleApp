using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Primus
{
   public class Octopus
    {
        #region class members

        public MarketTicker MarketDataSource { get; set; }
        public static Octopus _instance;
        private static readonly object _lock = new object();

        #endregion

        public Octopus(string authToken, string loginId, string feedurl)
        {
            MarketDataSource = new MarketTicker(feedurl);
            MarketDataSource.Connect();
            //MarketDataSource.CreateChannelAndSubscribeFromMap();
        }

        public MarketTicker GetWebSocketSource(string authToken, string loginId, string feedurl)
        {
            var dataSource = new MarketTicker(feedurl);
            dataSource.Connect();
            //dataSource.CreateChannelAndSubscribeFromMap();
            return dataSource;
        }

        public static Octopus Instance(string authToken, string loginId, string feedurl)
        {
            lock (_lock)
            {
                return _instance = (_instance) ?? new Octopus(authToken, loginId, feedurl);
            }
        }
    }
}
