using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Core;
using Core.Abstractions;
using Core.Helpers;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Cli
{
    abstract class Extend : IEntity
    {
        
    }
    class Obj2 : Extend
    {
        public string Name { get; set; }
        public List<Obj3> Obj3List { get; set; }
    }

    class Obj3 : Extend
    {
        public int Age { get; set; }
    }
    
    class Obj1 : Extend
    {
        public Obj2[] Obj2Array { get; set; }    
        public List<Obj2> Obj2List { get; set; }   
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            var url = new MongoUrl(
                "mongodb://localhost:27017/?readPreference=primary&appname=MongoDB%20Compass&ssl=false");
            
            var mongoClientSettings = MongoClientSettings.FromUrl(url);
            mongoClientSettings.ClusterConfigurator = cb => {
                cb.Subscribe<CommandStartedEvent>(e => {
                    Console.WriteLine($"[MONGO] {e.CommandName} - {e.Command.ToJson()}");
                });
            };
            
            var client = new MongoClient(mongoClientSettings);
            var db = client.GetDatabase("mongo_demo");
            var repo = new Repository<User>(db, "users");
            
            repo.Depopulate("Orders");
            repo.Depopulate("Usernames.Emails");
            repo.Logging = false;

            Stopwatch sw = Stopwatch.StartNew();
            
            sw.Restart();

            var user0 = repo.First();
            Console.WriteLine(sw.Elapsed);

            var user = repo.FirstBeta();
            Console.WriteLine(sw.Elapsed);
            sw.Restart();
            
            var user1 = repo.FirstBeta();
            Console.WriteLine(sw.Elapsed);
            sw.Restart();

            var user2 = repo.First();
            Console.WriteLine(sw.Elapsed);




            //Soooo difficult. just start from scratch with understanding reflection
            /*var obj = new Obj1()
            {
                Obj2List = new List<Obj2>()
                {
                    new Obj2()
                    {
                        Name = "Henk",
                        Obj3List = new List<Obj3>()
                    },
                    new Obj2()
                    {
                        Name = "Kees",
                        Obj3List = new List<Obj3>()
                        {
                            new Obj3()
                            {
                                Age = 5
                            },
                            new Obj3()
                            {
                                Age = 22
                            }
                        }
                    }
                },
                Obj2Array = new Obj2[]
                {
                    new Obj2()
                    {
                        Name = "Piet",
                        Obj3List = new List<Obj3>()
                        {
                            new Obj3()
                            {
                                Age = 16
                            }
                        }
                    }
                }
            };*/


        }

        
    }
}