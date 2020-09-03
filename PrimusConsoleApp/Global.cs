using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimusConsoleApp
{
    class Global
    {
        public static string LoginId;
        public static string Twofa_token;
        public static Dictionary<int, string> Questions = new Dictionary<int, string>();
        public static string AuthToken;
        public static string token;
        public static string exchange;
        public static string scriptname;
		public static string appCode;
    }
}
