using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Core;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;

namespace Cli
{
    class Program
    {
        static async Task Main(string[] args)
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
            var repo = new Repository<User>(db, "users")
            {
                Logging = false, 
                UseLookup = false, 
                DoPopulate = true
            };

            //repo.Depopulate("Orders");
            //repo.Depopulate("Usernames.Emails");


            Console.WriteLine("Starting");
            
            var first = repo.FindById("608bd8d802965973de535801");
            Console.WriteLine(first.ToJson());
            

            //Console.WriteLine(repo.First().ToJson());



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