﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using WebSocketSharp;
namespace Primus
{
    public delegate void PriceUpdateDelegate(FullMarketTick data);
    public delegate void OrderUpdateDelegate(OrderUpdate orderUpdate);
    public delegate void TradeUpdateDelegate(TradeUpdate tradeUpdate);


    public class MarketTicker
    {
        private string _baseUrl;
        private bool _isReconnect = false;
        private int _reconnectionInterval;
        private int _reconnectTries;
        private int _retryCount = 0;
        private System.Timers.Timer _timer;
        private int _tickInterval = 10000;
        private bool _debug = false;
        private WebSocket _webSocket;
        private bool IsConnected => _webSocket.ReadyState == WebSocketState.Open;
        private static readonly Dictionary<string, Dictionary<string, int>> _subscriptionMap = new Dictionary<string, Dictionary<string, int>>();
        public readonly Dictionary<string, string> SnapQuoteDictionary = new Dictionary<string, string>();
        public readonly Dictionary<string, Dictionary<string, string>> MarketStatusDictionary = new Dictionary<string, Dictionary<string, string>>();
        public delegate void ConnectionUpdateDelegate(bool isConnected, string message);
        #region Events

        public event ConnectionUpdateDelegate ConnectionUpdateEvent;
        private void OnConnectionUpdateEvent(bool isConnected, string message)
        {
            var handler = ConnectionUpdateEvent;
            handler?.Invoke(isConnected, message);
        }

        public event PriceUpdateDelegate PriceUpdateEvent;
        private void OnPriceFeedReceiveEvent(FullMarketTick price)
        {
            var handler = PriceUpdateEvent;
            handler?.Invoke(price);
        }

        public event OrderUpdateDelegate OrderUpdateEvent;
        private void OnOrderUpdateReceive(OrderUpdate orderUpdate)
        {
            var handler = OrderUpdateEvent;
            handler?.Invoke(orderUpdate);
        }

        public event TradeUpdateDelegate TradeUpdateEvent;
        private void OnTradeUpdateReceive(TradeUpdate tradeUpdate)
        {
            var handler = TradeUpdateEvent;
            handler?.Invoke(tradeUpdate);
        }

        //public event SnapQouteDataDelegate SnapQuoteDataUpdateEvent;
        //private void OnSnapQuoteUpdateEvent(SnapQuote snap)
        //{
        //    var handler = SnapQuoteDataUpdateEvent;
        //    handler?.Invoke(snap);
        //}

        public struct RequestStruct
        {
            public string a;
            public List<string> v;
            public string m;
        }

        #endregion


        private string _loginId;
        private string _deviceType;
        public MarketTicker(string baseurl, bool reconnect = true, int reconnectInterval = 5, int reconnectTries = 5000, bool debug = false)
        {
            _debug = debug;
            _isReconnect = reconnect;
            _reconnectionInterval = reconnectInterval;
            _reconnectTries = reconnectTries;
            _baseUrl = "wss://" + baseurl + "/ws/v1/feeds";
            _webSocket = new WebSocket(_baseUrl);
            _timer = new System.Timers.Timer(_tickInterval);
            _timer.Elapsed += _timer_Elapsed;
            _configure();

        }

        private void _configure()
        {
            _webSocket.Log.Level = LogLevel.Trace;
            _webSocket.OnOpen += OnSocketConnect;
            _webSocket.OnError += OnSocketError;
            _webSocket.OnClose += OnSocketClose;
            _webSocket.OnMessage += OnDataReceived;
        }



        public void ReSubscribeFromMap()
        {
            var subMap = new Dictionary<string, Dictionary<string, int>>();
            foreach (var x in _subscriptionMap)
            {
                var subList = new Dictionary<string, int>();
                foreach (var y in x.Value)
                {
                    subList.Add(y.Key, y.Value);
                }
                subMap[x.Key] = subList;
            }
            _subscriptionMap.Clear();

            foreach (string subType in subMap.Keys)
            {
                var exchangeTokenList = new List<Tuple<int, int>>();
                var exchangelist = new List<int>();
                switch (subType)
                {
                    case "mw":
                        foreach (var x in subMap["mw"])
                        {
                            string[] val = x.Key.Split('_');
                            exchangeTokenList.Add(Tuple.Create(Convert.ToInt32(val[0]), Convert.ToInt32(val[1])));
                        }
                        SubscribeMktPriceFeed(exchangeTokenList);
                        break;
                    case "sq":
                        foreach (var x in subMap["mw"])
                        {
                            string[] val = x.Key.Split('_');
                            exchangeTokenList.Add(Tuple.Create(Convert.ToInt32(val[0]), Convert.ToInt32(val[1])));
                        }
                        SubscribeSnapQuoteFeed(exchangeTokenList);
                        break;
                }
            }
        }

        #region Requests

        #region WebSocket connection Methods

        /// <summary>
        /// Starts a websocket connection and connects
        /// </summary>
        public void Connect()
        {
            if (!IsConnected)
            {
                _webSocket.Connect();

            }
        }

        /// <summary>
        /// Disconnect a websocket connection
        /// </summary>
        public void Disconnect()
        {
            if (IsConnected)
            {
                _webSocket.Close(CloseStatusCode.Away);
            }
        }

        /// <summary>
        /// Reconnects using a webscoket conmection
        /// </summary>
        public void Reconnect()
        {
            if (IsConnected)
            {
                _webSocket.Close(CloseStatusCode.Normal);
                _timer.Stop();
            }

            if (_retryCount < _reconnectTries)
            {
                _retryCount++;
                Connect();
            }
        }

        private void _sendHeartBeat()
        {
            string msg = "{\"a\":\"h\",\"v\":[], m:\"\"}";
            System.Diagnostics.Debug.WriteLine("Sending heartbeat message:" + msg);
            _webSocket.Send(msg);
        }

        #endregion

        #region Market price feed channel creation and price subscription

        /// <summary>
        /// Subscribe market feed
        /// </summary>
        /// <param name="exchange">must be int</param>
        /// <param name="token">must be int</param>
        public void SubscribeMktPriceFeed(List<Tuple<int, int>> exchangeToken, bool reconnection = false)
        {
            if (!IsConnected)
            {
                Console.WriteLine(@"state of the socket", @"closed");
                return;
            }
            var subscriptionList = new List<string>();
            foreach (var itemTuple in exchangeToken)
            {
                var key = itemTuple.Item1 + "_" + itemTuple.Item2;
                if (_subscriptionMap.ContainsKey("mw"))
                {
                    if (_subscriptionMap["mw"].ContainsKey(key))
                        _subscriptionMap["mw"][key] += 1;
                    else
                    {
                        _subscriptionMap["mw"][key] = 1;
                        subscriptionList.Add(" [" + itemTuple.Item1 + ", " + itemTuple.Item2 + " ]");
                    }
                }
                else
                {
                    _subscriptionMap["mw"] = new Dictionary<string, int> { [key] = 1 };
                    subscriptionList.Add(" [" + itemTuple.Item1 + ", " + itemTuple.Item2 + " ]");
                }
            }
            var subscriptionItem = string.Join(", ", subscriptionList);
            var msg = "{\"a\": \"subscribe\",\"v\":[" + subscriptionItem + "], \"m\": \"marketdata\"}";
            _webSocket.Send(msg);
        }

        /// <summary>
        /// UnSubscribes scrips from mkt watch
        /// </summary>
        /// <param name="exchangeToken">List of tumples of int having exchange, instrumenttoken</param>
        public void UnSubMktPriceFeed(List<Tuple<int, int>> exchangeToken)
        {
            if (!IsConnected)
            {
                Console.WriteLine(@"state of the socket", @"closed");
                return;
            }
            var unSubscriptionList = new List<string>();
            foreach (var itemTuple in exchangeToken)
            {
                var key = itemTuple.Item1 + "_" + itemTuple.Item2;
                if (_subscriptionMap.ContainsKey("mw"))
                {
                    if (_subscriptionMap["mw"].ContainsKey(key))
                    {
                        _subscriptionMap["mw"][key] -= 1;

                        if (_subscriptionMap["mw"][key] == 0)
                        {
                            unSubscriptionList.Add(" [" + itemTuple.Item1 + ", " + itemTuple.Item2 + " ]");
                            _subscriptionMap["mw"].Remove(key);
                        }
                    }
                    else
                        return;
                }
            }
            var unsubscriptionItems = string.Join(", ", unSubscriptionList);
            var msg = "{\"a\":\"unsubscribe\",\"v\":[" + unsubscriptionItems + "], \"m\": \"marketdata\"}";
            _webSocket.Send(msg);
        }

        #endregion

        #region Snap Quote: Channel made for 5 depth
        /// <summary>
        ///  Subscribe SnapQuote based on exchange and token
        /// </summary>
        /// <param name="exchangeToken">List of tumples of int having exchange, instrumenttoken</param>
        public void SubscribeSnapQuoteFeed(List<Tuple<int, int>> exchangeToken)
        {
            if (!IsConnected)
            {
                Console.WriteLine(@"state of the socket", @"closed");
                return;
            }
            var subscriptionList = new List<string>();
            foreach (var itemTuple in exchangeToken)
            {
                var key = itemTuple.Item1 + "_" + itemTuple.Item2;
                if (_subscriptionMap.ContainsKey("sq"))
                {
                    if (_subscriptionMap["sq"].ContainsKey(key))
                        _subscriptionMap["sq"][key] += 1;
                    else
                    {
                        _subscriptionMap["sq"][key] = 1;
                        subscriptionList.Add(" [" + itemTuple.Item1 + ", " + itemTuple.Item2 + " ]");
                    }
                }
                else
                {
                    _subscriptionMap["sq"] = new Dictionary<string, int> { [key] = 1 };
                    subscriptionList.Add(" [" + itemTuple.Item1 + ", " + itemTuple.Item2 + " ]");
                }
            }
            var subscriptionItem = string.Join(", ", subscriptionList);
            var msg = "{\"a\": \"subscribe\",\"v\":[" + subscriptionItem + "], \"m\": \"snapquote\"}";
            _webSocket.Send(msg);

        }

        ///// <summary>
        ///// UnSubscribe SnapQuote based on exchange and token
        ///// </summary>
        ///// <param name="exchange">must be int value</param>
        ///// <param name="token">must be int value</param>
        public void UnSubSnapQuoteFeed(List<Tuple<int, int>> exchangeToken)
        {
            if (!IsConnected)
            {
                Console.WriteLine(@"state of the socket", @"closed");
                return;
            }
            var unSubscriptionList = new List<string>();
            foreach (var itemTuple in exchangeToken)
            {
                var key = itemTuple.Item1 + "_" + itemTuple.Item2;
                if (_subscriptionMap.ContainsKey("sq"))
                {
                    if (_subscriptionMap["sq"].ContainsKey(key))
                    {
                        _subscriptionMap["sq"][key] -= 1;
                        if (_subscriptionMap["sq"][key] == 0)
                        {
                            _subscriptionMap["sq"].Remove(key);
                            unSubscriptionList.Add(" [" + itemTuple.Item1 + ", " + itemTuple.Item2 + " ]");
                        }
                    }
                    else
                        return;
                }
            }
            var unsubscriptionItem = string.Join(", ", unSubscriptionList);
            var msg = "{\"a\": \"unsubscribe\",\"v\":[" + unsubscriptionItem + "], \"m\": \"snapquote\"}";
            _webSocket.Send(msg);
        }


        public void SubscribeOrderTradeUpdates(string loginId, string deviceType)
        {
            _loginId = loginId;
            _deviceType = deviceType;
            RequestStruct req = new RequestStruct();
            req.a = "subscribe";
            req.v = new List<string>
            {
                loginId,
                deviceType
            };
            req.m = "updates";
            var json = JsonConvert.SerializeObject(req);
            _webSocket.Send(json);
        }

        #endregion

        #endregion

        #region callbacks

        private void OnSocketConnect(object sender, EventArgs e)
        {
            _timer.Start();
            _retryCount = 0;
            PrimusApi.LogFile(DateTime.Now + " : " + "Socket connected");
            OnConnectionUpdateEvent(true, "Socket Connected");
            if (_subscriptionMap.Keys.Count > 0)
            {
                ReSubscribeFromMap();
                SubscribeOrderTradeUpdates(_loginId, _deviceType);
            }
        }

        private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (IsConnected)
                _sendHeartBeat();
        }

        private void OnSocketClose(object sender, CloseEventArgs e)
        {
            PrimusApi.LogFile(DateTime.Now + " : " + "Socket closed");
            _timer.Stop();
            OnConnectionUpdateEvent(false, "Socket Closed");
            System.Threading.Thread.Sleep(1000);
            if (_isReconnect) Reconnect();
        }

        private void OnSocketError(object sender, ErrorEventArgs e)
        {
            PrimusApi.LogFile(DateTime.Now + " : " + "Socket Error occurred" + e.Message);
            OnConnectionUpdateEvent(IsConnected, e.Message);
        }

        private void OnDataReceived(object sender, MessageEventArgs e)
        {
            //if ( e.IsBinary)
            //{
            var data = e.RawData;
            var offset = 0;
            var mode = data[offset];
            offset++;
            switch (mode)
            {
                //market data
                case 1:
                    ReadMarketData(data, ref offset);
                    break;
                case 3:
                    ReadSnapQuote(data, ref offset);
                    break;
                case 50:
                case 11:
                    ReadOrderUpdateData(data, ref offset);
                    break;
                case 51:
                case 12:
                    ReadTradeUpdateData(data, ref offset);
                    break;

                default:
                    break;
                    //}
            }
        }
        #endregion

        #region Binary data parser

        private void ReadMarketData(byte[] data, ref int offset)
        {
            var length = data.Length - offset;
            IntPtr dataPacket = Marshal.AllocHGlobal(length);
            Marshal.Copy(data, offset, dataPacket, length);
            var marketData = (NewMarketData)Marshal.PtrToStructure(dataPacket, typeof(NewMarketData));
            Marshal.FreeHGlobal(dataPacket);
            var val = Twiddle(marketData);
            OnPriceFeedReceiveEvent(val);
        }

        private void ReadSnapQuote(byte[] data, ref int offset)
        {
            var length = data.Length - offset;
            IntPtr snapPacket = Marshal.AllocHGlobal(length);
            Marshal.Copy(data, offset, snapPacket, length);
            var snapData = (SnapQuote)Marshal.PtrToStructure(snapPacket, typeof(SnapQuote));
            Marshal.FreeHGlobal(snapPacket);
            var val = Twiddle(snapData);
            //OnSnapQuoteUpdateEvent(val);
        }

        private void ReadOrderUpdateData(byte[] data, ref int offset)
        {
            string orderUpdateJsonPacket = Encoding.UTF8.GetString(data.SubArray(5, data.Length - 5));

            var deserializedPacket = JsonConvert.DeserializeObject<Dictionary<string, object>>(orderUpdateJsonPacket);

            OrderUpdate order = new OrderUpdate();
            order.AverageTradePrice = Convert.ToDouble(deserializedPacket["average_price"]);
            order.ClientId = deserializedPacket["client_id"].ToString();
            order.DisclosedQuantity = Convert.ToInt32(deserializedPacket["disclosed_quantity"]);
            order.Exchange = deserializedPacket["exchange"].ToString();
            order.ExchangeOrderId = deserializedPacket["exchange_order_id"].ToString();
            order.ExchangeTime = Convert.ToInt64(deserializedPacket["exchange_time"]);
            order.FilledQuantity = Convert.ToInt32(deserializedPacket["filled_quantity"]);
            order.InstrumentToken = Convert.ToInt32(deserializedPacket["instrument_token"]);
            order.LoginId = deserializedPacket["login_id"].ToString();
            order.OmsOrderId = deserializedPacket["oms_order_id"].ToString();
            order.OrderEntryTime = Convert.ToInt64(deserializedPacket["order_entry_time"]);
            order.OrderStatus = deserializedPacket["order_status"].ToString();
            order.OrderType = deserializedPacket["order_type"].ToString();
            order.Price = Convert.ToDouble(deserializedPacket["price"]);
            order.Product = deserializedPacket["product"].ToString();
            order.Quantity = Convert.ToInt32(deserializedPacket["quantity"]);
            order.RejectionReason = deserializedPacket["rejection_reason"].ToString();
            order.RemainingQuantity = Convert.ToInt32(deserializedPacket["remaining_quantity"]);
            order.TradingSymbol = deserializedPacket["trading_symbol"].ToString();
            order.TriggerPrice = Convert.ToDouble(deserializedPacket["trigger_price"]);
            order.userOrderId = deserializedPacket["user_order_id"].ToString();
            order.Validity = deserializedPacket["validity"].ToString();
            order.OrderSide = deserializedPacket["order_side"].ToString();
            order.Procli = deserializedPacket["pro_cli"].ToString();
            order.nnfid = deserializedPacket["nnfid"].ToString();
            OnOrderUpdateReceive(order);
        }

        private void ReadTradeUpdateData(byte[] data, ref int offset)
        {
            string tradeUpdateJsonPacket = Encoding.UTF8.GetString(data.SubArray(5, data.Length - 5));
            var deserializedPacket = JsonConvert.DeserializeObject<Dictionary<string, object>>(tradeUpdateJsonPacket);
            TradeUpdate trade = new TradeUpdate();
            trade.Exchange = deserializedPacket["exchange"].ToString();
            trade.Product = deserializedPacket["product"].ToString();
            trade.TradingSymbol = deserializedPacket["trading_symbol"].ToString();
            trade.TradePrice = Convert.ToDouble(deserializedPacket["trade_price"]);
            trade.TradeNumber = deserializedPacket["trade_id"].ToString();
            trade.OrderSide = deserializedPacket["order_side"].ToString();
            trade.OmsOrderId = deserializedPacket["oms_order_id"].ToString();
            trade.LoginId = deserializedPacket["login_id"].ToString();
            trade.InstrumentToken = Convert.ToInt32(deserializedPacket["instrument_token"]);
            trade.TradeQuantity = Convert.ToInt32(deserializedPacket["filled_quantity"]);
            trade.TradeTime = Convert.ToInt64(deserializedPacket["exchange_time"]);
            trade.ExchangeOrderId = deserializedPacket["exchange_order_id"].ToString();
            trade.ClientId = deserializedPacket["client_id"].ToString();
            OnTradeUpdateReceive(trade);
        }

        public static byte[] FromHex(string hex)
        {
            hex = hex.Replace("-", "");
            byte[] raw = new byte[hex.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return raw;
        }

        
        

        #endregion

        #region Endianness change


        private static int Twiddle(int intVal)
        {
            var temp = BitConverter.GetBytes(intVal);
            if (BitConverter.IsLittleEndian) Array.Reverse(temp);
            intVal = BitConverter.ToInt32(temp, 0);
            return intVal;
        }

        private static short Twiddle(short shortVal)
        {
            var temp = BitConverter.GetBytes(shortVal);
            if (BitConverter.IsLittleEndian) Array.Reverse(temp);
            shortVal = BitConverter.ToInt16(temp, 0);
            return shortVal;
        }

        private static double Twiddle(double doubleVal)
        {
            var temp = BitConverter.GetBytes(doubleVal);
            if (BitConverter.IsLittleEndian) Array.Reverse(temp);
            doubleVal = BitConverter.ToDouble(temp, 0);
            return doubleVal;
        }

        private static long Twiddle(long longVal)
        {
            var temp = BitConverter.GetBytes(longVal);
            if (BitConverter.IsLittleEndian) Array.Reverse(temp);
            longVal = BitConverter.ToInt64(temp, 0);
            return longVal;
        }

        private FullMarketTick Twiddle(NewMarketData data)
        {
            var marketdata = new FullMarketTick();
            marketdata.Type = MarketDataType.MarketData;
            marketdata.Exchange = data.Exchange;
            marketdata.InstrumentToken = Twiddle(data.InstrumentToken);
            marketdata.AverageTradePrice = Twiddle(data.AverageTradePrice);
            marketdata.BestAskQty = Twiddle(data.BestAskQty);
            marketdata.BestBidPrice = Twiddle(data.BestBidPrice);
            marketdata.BestBidQty = Twiddle(data.BestBidQty);
            marketdata.BestAskPrice = Twiddle(data.BestAskPrice);
            marketdata.ExchangeTimeStamp = Twiddle(data.ExchangeTimeStamp);
            marketdata.ClosePrice = Twiddle(data.ClosePrice);
            marketdata.HighPrice = Twiddle(data.HighPrice);
            marketdata.OpenPrice = Twiddle(data.OpenPrice);
            marketdata.Volume = Twiddle(data.Volume);
            marketdata.LowPrice = Twiddle(data.LowPrice);
            marketdata.YearlyHigh = Twiddle(data.YearlyHigh);
            marketdata.YearlyLow = Twiddle(data.YearlyLow);
            marketdata.LastTradeQty = Twiddle(data.LastTradeQty);
            marketdata.LastTradeTime = Twiddle(data.LastTradeTime);
            marketdata.LastTradedPrice = Twiddle(data.LastTradedPrice);
            marketdata.TotalBuyQty = Twiddle(data.TotalBuyQty);
            marketdata.TotalSellQty = Twiddle(data.TotalSellQty);
            return marketdata;
        }

       

       

        private SnapQuote Twiddle(SnapQuote snapQuote)
        {
            snapQuote.InstrumentToken = Twiddle(snapQuote.InstrumentToken);

            snapQuote.Buyers[0] = Twiddle(snapQuote.Buyers[0]);
            snapQuote.Buyers[1] = Twiddle(snapQuote.Buyers[1]);
            snapQuote.Buyers[2] = Twiddle(snapQuote.Buyers[2]);
            snapQuote.Buyers[3] = Twiddle(snapQuote.Buyers[3]);
            snapQuote.Buyers[4] = Twiddle(snapQuote.Buyers[4]);

            snapQuote.BidPrice[0] = Twiddle(snapQuote.BidPrice[0]);
            snapQuote.BidPrice[1] = Twiddle(snapQuote.BidPrice[1]);
            snapQuote.BidPrice[2] = Twiddle(snapQuote.BidPrice[2]);
            snapQuote.BidPrice[3] = Twiddle(snapQuote.BidPrice[3]);
            snapQuote.BidPrice[4] = Twiddle(snapQuote.BidPrice[4]);

            snapQuote.BidQty[0] = Twiddle(snapQuote.BidQty[0]);
            snapQuote.BidQty[1] = Twiddle(snapQuote.BidQty[1]);
            snapQuote.BidQty[2] = Twiddle(snapQuote.BidQty[2]);
            snapQuote.BidQty[3] = Twiddle(snapQuote.BidQty[3]);
            snapQuote.BidQty[4] = Twiddle(snapQuote.BidQty[4]);


            snapQuote.Sellers[0] = Twiddle(snapQuote.Sellers[0]);
            snapQuote.Sellers[1] = Twiddle(snapQuote.Sellers[1]);
            snapQuote.Sellers[2] = Twiddle(snapQuote.Sellers[2]);
            snapQuote.Sellers[3] = Twiddle(snapQuote.Sellers[3]);
            snapQuote.Sellers[4] = Twiddle(snapQuote.Sellers[4]);

            snapQuote.AskPrice[0] = Twiddle(snapQuote.AskPrice[0]);
            snapQuote.AskPrice[1] = Twiddle(snapQuote.AskPrice[1]);
            snapQuote.AskPrice[2] = Twiddle(snapQuote.AskPrice[2]);
            snapQuote.AskPrice[3] = Twiddle(snapQuote.AskPrice[3]);
            snapQuote.AskPrice[4] = Twiddle(snapQuote.AskPrice[4]);

            snapQuote.AskQty[0] = Twiddle(snapQuote.AskQty[0]);
            snapQuote.AskQty[1] = Twiddle(snapQuote.AskQty[1]);
            snapQuote.AskQty[2] = Twiddle(snapQuote.AskQty[2]);
            snapQuote.AskQty[3] = Twiddle(snapQuote.AskQty[3]);
            snapQuote.AskQty[4] = Twiddle(snapQuote.AskQty[4]);

            snapQuote.ExchangeTimestamp = Twiddle(snapQuote.ExchangeTimestamp);
            return snapQuote;
        }

        #endregion

    }
    public enum Exchange
    {
        NSE = 1,
        NFO = 2,
        CDS = 3,
        MCX = 4,
        BSE = 6,
        BFO = 7
    }

    public enum Mode
    {
        marketdata,
        compact_marketdata,
        snapquote
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public struct NewMarketData
    {
        public byte Exchange;
        public Int32 InstrumentToken;
        public Int32 LastTradedPrice;
        public Int32 LastTradeTime;
        public Int32 LastTradeQty;
        public Int32 Volume;
        public Int32 BestBidPrice;
        public Int32 BestBidQty;
        public Int32 BestAskPrice;
        public Int32 BestAskQty;
        public Int64 TotalBuyQty;
        public Int64 TotalSellQty;
        public Int32 AverageTradePrice;
        public Int32 ExchangeTimeStamp;
        public Int32 OpenPrice;
        public Int32 HighPrice;
        public Int32 LowPrice;
        public Int32 ClosePrice;
        public Int32 YearlyHigh;
        public Int32 YearlyLow;
        public Int32 LowDPR;
        public Int32 HighDPR;
        public Int32 CurrentOI;
        public Int32 InitialOI;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public struct SnapQuote
    {
        public byte Exchange;
        public Int32 InstrumentToken;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public Int32[] Buyers;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public Int32[] BidPrice;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public Int32[] BidQty;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public Int32[] Sellers;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public Int32[] AskPrice;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public Int32[] AskQty;

        public Int32 ExchangeTimestamp;
    }

 

    public class FullMarketTick
    {
        public byte Exchange;
        public Int32 InstrumentToken;
        public Int32 LastTradedPrice;
        public Int32 LastTradeTime;
        public Int32 LastTradeQty;
        public Int32 Volume;
        public Int32 BestBidPrice;
        public Int32 BestBidQty;
        public Int32 BestAskPrice;
        public Int32 BestAskQty;
        public Int64 TotalBuyQty;
        public Int64 TotalSellQty;
        public Int32 AverageTradePrice;
        public Int32 ExchangeTimeStamp;
        public Int32 OpenPrice;
        public Int32 HighPrice;
        public Int32 LowPrice;
        public Int32 ClosePrice;
        public Int32 YearlyHigh;
        public Int32 YearlyLow;
        public Int32 HighCircuitLimit;
        public Int32 LowCircuitLimit;
        public Int32 CurrentOpenInterest;
        public Int32 InitialOpenInterest;
        public MarketDataType Type;
    }
    public enum MarketDataType
    {
        MarketData
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public class OrderUpdate
    {
        public string userOrderId;
        public int RejectionCode;
        public double TriggerPrice;
        public int RemainingQuantity;
        public string OrderType;
        public string OrderSide;
        public string OrderStatus;
        public long ExchangeTime;
        public string Device;
        public long OrderEntryTime;
        public string TradingSymbol;
        public int FilledQuantity;
        public double Deposit;
        public double Price;
        public int InstrumentToken;
        public string LoginId;
        public string ClientId;
        public string ExchangeOrderId;
        public string Validity;
        public int Quantity;
        public double TradePrice;
        public string RejectionReason;
        public string Exchange;
        public int DisclosedQuantity;
        public double AverageTradePrice;
        public string OmsOrderId;
        public string Product;
        public string Procli;
        public string nnfid;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable]
    public class TradeUpdate
    {
        public string ClientId;
        public string BookType;
        public string BrokerId;
        public string DisclosedVaol;
        public string DisclosedVolRemaining;
        public string Exchange;
        public string ExchangeOrderId;
        public string ExchangeTime;
        public int FillNumber;
        public int FilledQty;
        public string GoodTillDate;
        public int InstrumentToken;
        public string LoginId;
        public string OmsOrderId;
        public string OrderEntryTime;
        public double OrderPrice;
        public string OrderSide;
        public string OrderType;
        public string OriginalVol;
        public string PAN;
        public string propOrClient;
        public string Product;
        public int RemQty;
        public string TradeNumber;
        public double TradePrice;
        public int TradeQuantity;
        public long TradeTime;
        public string TradingSymbol;
        public double TriggerPrice;
        public string VolFilledToday;
    }
}
