using DbModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace credittrade.ViewModel
{
    public class PaymentViewModel
    {
        public string name;
        public string amount;
        public string edizm;
        public string price;
        public string cost;
    }
    public class PayViewModel
    {
        public List<PaymentViewModel> payments;
        public string id;
        public string cost;
        public string type;
        public string date;
    }
    public class RequestPartialsViewModel
    {
        public string id;
        public user user;
        public string buyerFio;
        public string date;
        public string cost;
        public string paid_cost;
        public string returned_cost;
        public List<PayViewModel> pays;
    }
}