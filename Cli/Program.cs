using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Core;
using Core.Abstractions;
using Core.Helpers;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            var url = new MongoUrl(
                "mongodb://localhost:27017/?readPreference=primary&appname=MongoDB%20Compass&ssl=false");
            
            var mongoClientSettings = MongoClientSettings.FromUrl(url);
            mongoClientSettings.ClusterConfigurator = cb => {
                cb.Subscribe<CommandStartedEvent>(e => {
                    //Console.WriteLine($"[MONGO] {e.CommandName} - {e.Command.ToJson()}");
                });
            };
            
            var client = new MongoClient(mongoClientSettings);
            var db = client.GetDatabase("mongo_demo");
            var repo = new Repository<User>(db, "users");
            
            //repo.Depopulate("Orders");
            //repo.Depopulate("Usernames.Emails");
            repo.Logging = false;

            repo.UseLookup = false;
            repo.DoPopulate = true;

            repo.Collection.Find(x => true).First();

            Console.WriteLine("Starting");
            
            var sw = Stopwatch.StartNew();
            var user = repo.FirstBeta();
            Console.WriteLine(sw.Elapsed);
            
            //About 0.02 - 0.076 - 0.09 (with aggregate)
            //The aggregate seems to be slower!
            
            Console.WriteLine(user.ToJson());


            //Console.WriteLine(repo.FirstBeta().ToJson());



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