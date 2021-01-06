using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Primus
{
    public class PrimusApi
    {
        #region Class Members
        private static readonly object _lock = new object();
        private static PrimusApi _instance;
        private HttpClient _httpClient;
        private string _apiUrl = "";
        private static string path = string.Empty;

        public struct Question_Ans
        {
            public int question_id;
            public string answer;
        }

        private static Dictionary<string, string> _routeMap = new Dictionary<string, string>
        {
            {"Login", "v1/user/login" },
            { "SubmitTwoFa", "v1/user/twofa" },
            { "Profile", "v1/user/profile?client_id=" },
            { "Search", "v1/search?key="},
            { "Scripinfo","v1/contract" },// /NSE?info=scrip&token=22
            { "MarketData","v1/marketdata" },// /NSE/Capital?symbol=ACC
            { "Holdings", "v1/holdings?client_id="},
            { "PendingOrders", "v1/orders?type=pending&client_id="},
            { "CompletedOrders", "v1/orders?type=completed&client_id="},
            { "OrderHistory", "/v1/order"},//  /omsOrderNum/history?client_id=
            { "Trades", "v1/trades?client_id="},
            { "DayPositions", "v1/positions?type=live&client_id="},
            { "NetPositions", "v1/positions?type=historical&client_id="},
            { "Cash", "v1/funds/view?type=all&client_id="},
            { "PlaceOrder", "v1/orders"},
            { "ModifyOrder", "v1/orders"},
            { "CancelOrder", "v1/orders"},//   /omsOrderNum?client_id="
            {"SearchScript", "v1/search?key="},
            {"CashPosition","v1/funds/view?client_id=" },
            
        };
        #endregion

        public PrimusApi(Uri baseurl)
        {
            _apiUrl = baseurl.AbsoluteUri + "/api/";
            _httpClient = new HttpClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            _httpClient.Timeout = TimeSpan.FromSeconds(240);
            path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\ApiLog.txt";
            if(File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public static PrimusApi Instance(Uri baseurl)
        {
            lock (_lock)
            {
                return _instance = _instance ?? new PrimusApi(baseurl);
            }
        }

        #region API calls
       
        public void SetAuthenticationToken(string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
        }

        public async Task<string> PlaceOrder(string exchange,string client_id, int instrument_token, int quantity,
            int disclosedQty, decimal price, decimal triggerPrice, int market_protection_percentage,
            string product, string order_side, string validity, string orderType, string user_order_id,string device="web")
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var values = new Dictionary<string, dynamic>
            {
                {"exchange", exchange},
                {"client_id",client_id },
                {"instrument_token", instrument_token},
                {"quantity", quantity},
                {"disclosed_quantity", disclosedQty},
                {"price", price},
                {"trigger_price", triggerPrice},
                {"market_protection_percentage",market_protection_percentage },
                {"product",product},
                {"order_side",order_side },
                {"validity", validity},
                {"order_type", orderType},
                {"user_order_id", user_order_id},
                {"device",device}
            };
            var jsonString = JsonSerialize(values);
            var buffer = Encoding.UTF8.GetBytes(jsonString);

            using (var byteContent = new ByteArrayContent(buffer))
            {
                byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                var response = await _httpClient.PostAsync(_apiUrl + _routeMap["PlaceOrder"], byteContent);
                var responseString = await response.Content.ReadAsStringAsync();
                stopwatch.Stop();
                LogFile(_apiUrl + _routeMap["PlaceOrder"] + "  : " + stopwatch.ElapsedMilliseconds + "ms");
                // OnOrderReply(responseString);
                return responseString;
            }
        }

        public async Task<string> ModifyOrder(string exchange, string client_id, int instrument_token, int quantity,
            int disclosedQty, decimal price, decimal triggerPrice, string oms_order_id,
            string product, string validity, string orderType)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var values = new Dictionary<string, dynamic>
            {
                {"exchange", exchange},
                {"client_id",client_id },
                {"instrument_token", instrument_token},
                {"quantity", quantity},
                {"disclosed_quantity", disclosedQty},
                {"price", price},
                {"trigger_price", triggerPrice},
                {"oms_order_id",oms_order_id },
                {"product",product},
             
                {"validity", validity},
                {"order_type", orderType}
            };
            var jsonString = JsonSerialize(values);
            var buffer = Encoding.UTF8.GetBytes(jsonString);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            var response = await _httpClient.PutAsync(_apiUrl + _routeMap["ModifyOrder"], byteContent);
            var responseString = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            LogFile(_apiUrl + _routeMap["ModifyOrder"] + "  : " + stopwatch.ElapsedMilliseconds + "ms");

            return responseString;
        }

        public async Task<string> CancelOrderAsync(string omsorderid, string client_id)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            string values = "/" + omsorderid + "?client_id=" + client_id;
            var response = await _httpClient.DeleteAsync(_apiUrl + _routeMap["CancelOrder"] + values);
            var responseString = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            LogFile(_apiUrl + _routeMap["CancelOrder"] + values + "  : " + stopwatch.ElapsedMilliseconds + "ms");

            return responseString;
        }


        public async Task<string> PendingOrderBookAsync(string client_id)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await _httpClient.GetAsync(_apiUrl + _routeMap["PendingOrders"]+ client_id);
            var responseString = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            LogFile(_apiUrl + _routeMap["PendingOrders"] + client_id + "  : " + stopwatch.ElapsedMilliseconds + "ms" );
            return responseString;
        }
        public async Task<string> CompletedOrderBookAsync(string client_id)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var url = (_apiUrl +_routeMap["CompletedOrders"] + client_id);
            var response = await _httpClient.GetAsync(_apiUrl + _routeMap["CompletedOrders"] + client_id);
            var responseString = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            LogFile(_apiUrl + _routeMap["CompletedOrders"] + client_id + "  : " + stopwatch.ElapsedMilliseconds + "ms");
            return responseString;
        }

        public async Task<string> GetProfileAsync(string client_id)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await _httpClient.GetAsync(_apiUrl + _routeMap["Profile"] + client_id);
            var responseString = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            LogFile(_apiUrl + _routeMap["Profile"] + client_id + "  : " + stopwatch.ElapsedMilliseconds + "ms");

            return responseString;
        }

        public async Task<string> OrderHistoryAsync(string omsOrderId,string client_id)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var values = "/" + omsOrderId + "/history?client_id="+client_id;
            var response = await _httpClient.GetAsync(_apiUrl + _routeMap["OrderHistory"] +  values);
            var responseString = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            LogFile(_apiUrl + _routeMap["OrderHistory"] + values + "  : " + stopwatch.ElapsedMilliseconds + "ms");

            return responseString;
        }

        public async Task<string> TradesAsync(string client_id)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await _httpClient.GetAsync(_apiUrl + _routeMap["Trades"] + client_id);
            var responseString = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            LogFile(_apiUrl + _routeMap["Trades"] + client_id + "  : " + stopwatch.ElapsedMilliseconds + "ms");

            return responseString;
        }

        public async Task<string> DayPositionsAsync(string client_id)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await _httpClient.GetAsync(_apiUrl + _routeMap["DayPositions"] + client_id);
            var responseString = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            LogFile(_apiUrl + _routeMap["DayPositions"] + client_id + "  : " + stopwatch.ElapsedMilliseconds + "ms");

            return responseString;
        }

        public async Task<string> NetPositionsAsync(string client_id)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await _httpClient.GetAsync(_apiUrl + _routeMap["NetPositions"] + client_id);
            var responseString = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            LogFile(_apiUrl + _routeMap["NetPositions"] + client_id + "  : " + stopwatch.ElapsedMilliseconds + "ms");

            return responseString;
        }


        public async Task<string> HoldingsAsync(string client_id)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await _httpClient.GetAsync(_apiUrl + _routeMap["Holdings"] + client_id);
            var responseString = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            LogFile(_apiUrl + _routeMap["Holdings"] + client_id + "  : " + stopwatch.ElapsedMilliseconds + "ms");

            return responseString;
        }

        public async Task<string> CashAsync(string client_id)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await _httpClient.GetAsync(_apiUrl + _routeMap["Cash"] + client_id);
            var responseString = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            LogFile(_apiUrl + _routeMap["Cash"] + client_id + "  : " + stopwatch.ElapsedMilliseconds + "ms");

            return responseString;
        }

        public async Task<string> SearchAsync(string key)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await _httpClient.GetAsync(_apiUrl + _routeMap["Search"] + key);
            var responseString = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            LogFile(_apiUrl + _routeMap["Search"] + key + "  : " + stopwatch.ElapsedMilliseconds + "ms");

            return responseString;
        }

        public async Task<string> ScripinfoAsync(string exchange, string token)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            string values = "/" + exchange + "?info=scrip&token=" + token;
            var response = await _httpClient.GetAsync(_apiUrl + _routeMap["Scripinfo"] + values);
            var responseString = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            LogFile(_apiUrl + _routeMap["Scripinfo"] + values + "  : " + stopwatch.ElapsedMilliseconds + "ms");

            //OnScripinfoReply(responseString);
            return responseString;
        }

        public async Task<string> MktdataAsync(string exchange, string segment,string symbol)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            string values = "/" + exchange + "/" + segment+"?symbol=" + symbol;
            var response = await _httpClient.GetAsync(_apiUrl + _routeMap["MarketData"] + values);
            var responseString = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            LogFile(_apiUrl + _routeMap["MarketData"] + values + "  : " + stopwatch.ElapsedMilliseconds + "ms");

            //OnScripinfoReply(responseString);
            return responseString;
        }

        public async Task<string> SearchScript(string Keyword)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var response = await _httpClient.GetAsync(_apiUrl + _routeMap["SearchScript"] + Keyword);
            var responseString = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            LogFile(_apiUrl + _routeMap["SearchScript"] + Keyword + "  : " + stopwatch.ElapsedMilliseconds + "ms");

            return responseString;
        }

        //public async Task<string> ScriptInfo(string Exchange, string token)
        //{
        //    string f = _apiUrl + _routeMap["ScriptInfo"] + Exchange + "?info=scrip&token=" + token;

        //    var response = await _httpClient.GetAsync(_apiUrl + _routeMap["ScriptInfo"] + Exchange + "?info=scrip&token=" + token);
        //    var responseString = await response.Content.ReadAsStringAsync();
        //    return responseString;
        //}

        public async Task<string> CashPosition(string client_id)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            //var url = (_apiUrl + _routeMap["CashPosition"] + client_id + "&type=all");
            var response = await _httpClient.GetAsync(_apiUrl + _routeMap["CashPosition"] + client_id + "&type=all");
            var responseString = await response.Content.ReadAsStringAsync();
            stopwatch.Stop();
            LogFile(_apiUrl + _routeMap["CashPosition"] + client_id + "&type=all" + "  : " + stopwatch.ElapsedMilliseconds + "ms");

            return responseString;
        }

        public static void LogFile(string item)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(item);
                sb.Append(Environment.NewLine);
                File.AppendAllText(path , sb.ToString());
                sb.Clear();
            }
            catch(Exception ex)
            {

            }
        }

        #endregion

        #region utility
        public static string JsonSerialize(object response)
        {
            return JsonConvert.SerializeObject(response);
        }
        #endregion
    }
}
