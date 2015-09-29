using Microsoft.Azure.ActiveDirectory.GraphClient;
using System.Collections.Generic;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using System;
using System.Threading.Tasks;

namespace MyADSampleDataApp.GenUsers
{
    public class Name
    {
        public string title { get; set; }
        public string first { get; set; }
        public string last { get; set; }
    }

    public class Location
    {
        public string street { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zip { get; set; }
    }

    public class Picture
    {
        public string large { get; set; }
        public string medium { get; set; }
        public string thumbnail { get; set; }
    }

    public class User 
    {
        public string gender { get; set; }
        public Name name { get; set; }
        public Location location { get; set; }
        public string email { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string salt { get; set; }
        public string md5 { get; set; }
        public string sha1 { get; set; }
        public string sha256 { get; set; }
        public int registered { get; set; }
        public int dob { get; set; }
        public string phone { get; set; }
        public string cell { get; set; }
        public string NINO { get; set; }
        public Picture picture { get; set; }
    }

    public class Result
    {
        public User user { get; set; }
    }

    public class RootObject
    {
        public List<Result> results { get; set; }
        public string nationality { get; set; }
        public string seed { get; set; }
        public string version { get; set; }
    }
}
