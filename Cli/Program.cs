using System;
using Core;
using Core.Helpers;
using MongoDB.Driver;

namespace Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            //var client = new MongoClient("mongodb://localhost:27017/?readPreference=primary&appname=MongoDB%20Compass&ssl=false");
            //var db = client.GetDatabase("mongo_demo");
            //var col = new Repository<User>(db.GetCollection<User>("users"));
            var col = new Repository<User>(null);
            col.debug();
        }
    }
}