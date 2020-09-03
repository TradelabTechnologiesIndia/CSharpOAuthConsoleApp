using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimusConsoleApp
{
    public struct LoginResponse
    {
        public string alert;
        public string auth_token;
        public List<question_str> questions;
        public bool reset_password;
        public bool reset_two_fa;
        public bool twofa_enabled;
        public string twofa_token;
    }

    public struct question_str
    {
        public string question;
        public int question_id;
    }

    public struct TwoFaResponseSuccess
    {
        public string auth_token;
        public bool reset_password;
        public bool reset_two_fa;
    }

   

}
