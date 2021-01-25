using Primus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Diagnostics;
using RestSharp;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Data;

namespace PrimusConsoleApp
{
    class Program
    {
        private static string AppID = "appid"; // Get from your broker
        private static string AppSecret = "app_secret"; // Get from your broker
        private static string redirect_url = "http://127.0.0.1/";  // You can create your own and ask your broker to update
        private static string base_url = "https://example.com/"; // Get from your broker;it will be in the form of https://api.example.com
        private static string scope = "orders holdings";
        private static readonly object _lock = new object();
        private static string url = null;
        public static Octopus _octopusInstance;
        [STAThread]
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            base_url = base_url.Trim('/', '\\');
            url = base_url + "/oauth2/auth?scope=" + scope + "&state=%7B%22param%22:%22value%22%7D&redirect_uri=" + redirect_url + "&response_type=code&client_id=" + AppID;

            Console.WriteLine("Press any key to navigate to \n" + url + "\n");
            Console.ReadLine();
            frmLogin login;
            //Process.Start(url);
            Application.Run(login = new frmLogin(url));

            //Console.WriteLine("Please enter userId, passoword and 2FA to login in the browser opened \n" + "Once login successful in the browser, please copy the code from browser::");
            var code = Global.appCode;
            var authbasic = Base64Encode(AppID + ":" + AppSecret);

            var client = new RestClient(base_url + "/oauth2/token");

            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddHeader("Authorization", "Basic " + authbasic);
            request.AddParameter("grant_type", "authorization_code");
            request.AddParameter("code", code);
            request.AddParameter("redirect_uri", redirect_url);
            request.AddParameter("client_id", AppID);
            IRestResponse response = client.Execute(request);
            var response_access = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(response.Content);
            if (response_access.Keys.Count > 3)
                Global.AuthToken = response_access["access_token"];
            var api = new Primus.PrimusApi(new Uri(base_url));
            api.SetAuthenticationToken(Global.AuthToken);
            Global.LoginId = login.CLientId;
            _octopusInstance = new Octopus(Global.AuthToken, Global.LoginId, new Uri(base_url).Host);
            _octopusInstance.MarketDataSource.PriceUpdateEvent += MarketDataSource_PriceUpdateEvent;
            _octopusInstance.MarketDataSource.SubscribeOrderTradeUpdates(Global.LoginId, "web");
            _octopusInstance.MarketDataSource.OrderUpdateEvent += MarketDataSource_OrderUpdateEvent;
            _octopusInstance.MarketDataSource.TradeUpdateEvent += MarketDataSource_TradeUpdateEvent;

            while (true)
            {
                Console.WriteLine("## Press following keys to perform activities ## \n " +
                    "Press 1 to Place Order \n " +
                    "Press 2 to Fetch Orderbook \n " +
                    "Press 3 to Fetch Tradebook \n " +
                    "Press 4 to Fetch Positions \n" +
                    "Press 5 to Fetch Holdings \n" +
                    "Press 6 to Fetch Search Script \n" +
                    "Press 7 to Fetch Script Info \n" +
                    "Press 8 to Subcribe for feeds \n" +
                    "Press 9 to Fetch CashPositions \n" +
                    "Press 0 to Exit"
                    );
                try

                {
                    var ch = Convert.ToInt16(Console.ReadLine());
                    switch (ch)
                    {
                        case 0:
                            Environment.Exit(0);
                            break;
                        case 1:
                            Console.WriteLine("\nPlacing Market order on ACC-EQ of exchange NSE\n");
                            var orderResponse = await api.PlaceOrder("NSE", Global.LoginId.ToUpper(), 22, 1, 0, 0, 0, 5, "MIS", "BUY", "DAY", "MARKET", "1001");
                            /* (string exchange,string client_id, int instrument_token, int quantity,
                            int disclosedQty, decimal price, decimal triggerPrice, int market_protection_percentage,string product, 
                            string order_side, string validity, string orderType, string user_order_id) */
                            var response1 = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(orderResponse);
                            if (response1["status"] == "success")
                            {
                                var output = DateTime.Now + ": " + response1["message"] + " oms_order_id: " + response1["data"]["client_order_id"];

                                Console.WriteLine(output + "\n\n");
                            }
                            break;

                        case 2:
                            var pendingOrderBookResponse = await api.PendingOrderBookAsync(Global.LoginId.ToUpper());
                            var response3 = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(pendingOrderBookResponse);
                            if (response3["status"] == "success")
                            {
                                Console.WriteLine(response3["data"]);

                            }

                            var CompletedOrderbookResponse = await api.CompletedOrderBookAsync(Global.LoginId.ToUpper());
                            var response33 = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(CompletedOrderbookResponse);
                            if (response33["status"] == "success")
                            {
                                Console.WriteLine(response33["data"]);
                            }
                            break;

                        case 3:
                            var tradeResponse = await api.TradesAsync(Global.LoginId.ToUpper());
                            var response4 = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(tradeResponse);
                            if (response4["status"] == "success")
                            {
                                Console.WriteLine(response4["data"]);
                            }
                            break;
                        case 4:
                            var posResponse = await api.DayPositionsAsync(Global.LoginId.ToUpper());
                            var response5 = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(posResponse);
                            if (response5["status"] == "success")
                            {
                                Console.WriteLine(response5["data"]);
                            }

                            var netPosResponse = await api.NetPositionsAsync(Global.LoginId.ToUpper());
                            var response55 = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(netPosResponse);
                            if (response55["status"] == "success")
                            {
                                Console.WriteLine(response55["data"]);
                            }

                            break;
                        case 5:
                            var holdingsResponse = await api.HoldingsAsync(Global.LoginId.ToUpper());
                            var response6 = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(holdingsResponse);
                            if (response6["status"] == "success")
                            {
                                Console.WriteLine(response6["data"]);
                            }
                            break;
                        case 6:
                            Console.WriteLine("Please enter ScriptName");
                            Global.scriptname = Console.ReadLine();
                            var SearchScriptResponse = await api.SearchScript(Global.scriptname.ToUpper());
                            var response7 = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(SearchScriptResponse);
                            if (response7.Count > 0)
                            {
                                Console.WriteLine(response7["result"]);
                            }
                            break;
                        case 7:
                            Console.WriteLine("Please enter Exchange");
                            Global.exchange = Console.ReadLine();
                            Console.WriteLine("Please enter Token");
                            Global.token = Console.ReadLine();
                            var ScriptInfoResponse = await api.ScripinfoAsync(Global.exchange.ToUpper(), Global.token);
                            var response8 = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(ScriptInfoResponse);
                            if (response8.Count > 0)
                            {
                                Console.WriteLine(response8["result"]);
                            }
                            break;
                        case 8:
                            Console.WriteLine("Please enter Exchange");
                            Global.exchange = Console.ReadLine();
                            Console.WriteLine("Please enter Token");
                            Global.token = Console.ReadLine();
                            var exchange = (Exchange)Enum.Parse(typeof(Exchange), Global.exchange.ToUpper());
                            SubscribeScripFeed(exchange, Convert.ToInt32(Global.token));
                            break;

                        case 9:
                            var cashresponse = await api.CashPosition(Global.LoginId);
                            var response9 = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(cashresponse);
                            if (response9["status"] == "success")
                            {
                                Console.WriteLine(response9["data"]);
                            }
                            break;

                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine("Invalid input ");
                    Environment.Exit(0);
                }

            }
        }
        #region Utility

        public static T JsonDeserialize<T>(string response)
        {
            var jObject = JsonConvert.DeserializeObject<T>(response);
            return jObject;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static void SubscribeScripFeed(Exchange exchange, int instrumentToken)
        {
            int val = 0;
            int.TryParse(Convert.ToString(instrumentToken), out val);
            var instrumentTuple = new List<Tuple<int, int>>();
            instrumentTuple.Add(Tuple.Create((int)exchange, instrumentToken));
            _octopusInstance.MarketDataSource.SubscribeMktPriceFeed(instrumentTuple);
        }

        private static void MarketDataSource_PriceUpdateEvent(FullMarketTick feed)
        {

            if (string.IsNullOrEmpty(feed.ToString())) return;

            var data = new MarketData
            {
                Exchange = feed.Exchange,
                InstrumentCode = feed.InstrumentToken,
                BidPrice = feed.BestBidPrice,
                BidQty = feed.BestBidQty,
                AskPrice = feed.BestAskPrice,
                AskQty = feed.BestAskQty,
                LastTradePrice = feed.LastTradedPrice,
                LastTradeQuantity = feed.LastTradeQty,
                ExchangeTimestamp = feed.ExchangeTimeStamp,
                LastTradeTime = feed.LastTradeTime,
                OpenPrice = feed.OpenPrice,
                HighPrice = feed.HighPrice,
                LowPrice = feed.LowPrice,
                ClosePrice = feed.ClosePrice,
                TotalBuyQty = feed.TotalBuyQty,
                TotalSellQty = feed.TotalSellQty,
                YearlyHigh = feed.YearlyHigh,
                YearlyLow = feed.YearlyLow,
                AvgTradePrice = feed.AverageTradePrice,
                TradeVolume = feed.Volume
            };

           string exg =  getExchange(data.Exchange.ToString());
            Console.WriteLine("Exchange " + exg +" Token "+ data.InstrumentCode + " LTP " + data.LastTradePrice + " LTQ " + data.LastTradeQuantity + " Volume " + data.TradeVolume);
        }

        private static string getExchange(string exchange)
        {
            switch(exchange)
            {
                case "1":
                    return "NSE";
                case "2":
                    return "NFO";
                case "3":
                    return "CDS";
                case "4":
                    return "MCX";
                case "6":
                    return "BSE";
                case "7":
                    return "BFO";
            }
            return "";
        }

        public class MarketData
        {
            public int Precision;
            public int multiplier;
            public int BidPrice;
            public int BidQty;
            public int AskPrice;
            public int AskQty;
            public int Exchange;
            public char[] TradingSymbol;
            public int InstrumentCode;
            public int LastTradePrice;
            public int LastTradeQuantity;
            public int ExchangeTimestamp;
            public int LastTradeTime;
            public long LowDpr;
            public long HighDpr;
            public int OpenPrice;
            public int ClosePrice;
            public int HighPrice;
            public int LowPrice;
            public long TotalBuyQty;
            public long TotalSellQty;
            public int YearlyHigh;
            public int YearlyLow;
            public int AvgTradePrice;
            public int CurrentOpenInterest;
            public int InitialOpenInterest;
            public int ChangeOpenInterest;
            public int TradeVolume;
        };
        #endregion

        #region Order and Trade updates
        private static void MarketDataSource_TradeUpdateEvent(TradeUpdate tradeUpdate)
        {
            Console.WriteLine("Client Id " + tradeUpdate.ClientId + " Product Type " + tradeUpdate.Product + " Order Type " + tradeUpdate.OrderType + " Trade Price " + tradeUpdate.TradePrice + " Traded Qty " + tradeUpdate.TradeQuantity + " Filled Qty " + tradeUpdate.FilledQty);
        }

        private static void MarketDataSource_OrderUpdateEvent(OrderUpdate orderDetail)
        {
            if (orderDetail.OrderStatus != "ACCEPTED")
            {
                Console.WriteLine(orderDetail.OrderStatus + " for Client Id " + orderDetail.ClientId + " Product Type " + orderDetail.Product + " Order Type " + orderDetail.OrderType + " Price " + orderDetail.Price + " Avg Price " + orderDetail.AverageTradePrice + " Trigger Price " + orderDetail.TriggerPrice + " Qty " + orderDetail.Quantity + " Disc Qty " + orderDetail.DisclosedQuantity);
            }
        }
        #endregion

    }

}
