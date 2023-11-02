using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WebApplication2.Models
{
    public class SubconModel
    {
        public SubconName SelectSubconName { get; set; }
    }

    public enum SubconName
    {
        asem, daeduckk, eastern, kinsus, nanya, semco, shinko, simtech, umtc, Simmtech_Japan, Simmtech_Korea, Simmtech_Penang
    }

}