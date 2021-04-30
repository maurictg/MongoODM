# MongoDB Object-Document-Mapper

## About
MongoODM is a object-document-mapper and a object-relational-mapper for MongoDB based on the C# driver. This library allows you to create and populate relations between documents. It also provides some easy methods to create, read, update and delete entities in the repository.

## Installation
To install MongoODM, install `MongoODM` from [NuGet](https://www.nuget.org/packages/MongoODM/) or use the dotnet cli:
```
dotnet add package MongoODM
```

Make sure you also have the official MongoDb driver, `MongoDB.Driver`, installed.

## Setup
### Create a domain model
To create a domain model, create a class that inherits the `Entity` class. It automatically contains an `Id` field of type string which is set up to be stored as an ObjectId.

```cs
//Domain class example
class Animal : Entity 
{
    //Contains already an Id field
    public string Name { get;set; }

    [Embed]
    public Diet AnimalDiet { get; set; }

    [Reference("caretakers", "CaretakerRef")]
    public Person Caretaker { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string CaretakerRef { get; set; }
}
```
To read more about the used attributes, take a look at the [relations page](#relations).


### Connect to a database
To connect to a MongoDB database, use the [official MongoDB Driver syntax](https://mongodb.github.io/mongo-csharp-driver/2.12/getting_started/quick_tour/):
```cs
var client = new MongoClient("<your connection string>");

//Then connect to a database, for example named animal_database
var database = client.GetDatabase("animal_database");
```

The API provides repositories to interact with the database and to map relations. To use the repository, create a new instance or create a class that inherits the repository of the specified type.
```cs
//Create a repository
IMongoRepository<Animal> animals = new MongoRepository<Animal>(database, "animals");
```
**Make sure that you are using an instance of the IMongoRepository and not the MongoRepository instance, because the IMongoRepository interface contains default methods to make your life easier.**

The constructor accepts two arguments: the database and the collection name. To read more about the repository instance read the [repository chapter](#Repository).

## CRUD functionality
The IMongoRepository provides a friendly interface to interact with the database. It has full support for CRUD (Create, Read, Update and Delete) operations.

### Add documents to the database
You can insert one document or multiple at once into the database.
```cs
    var elephant = new Animal() {
        Name = "Elephant",
        Diet = new Diet() {
            Amount = 1000,
            Name = "Peanuts"
        }
    };

    //Add one document to the database
    animals.Insert(elephant);

    //Insert multiple documents or collection of documents
    animals.InsertMany(elepant, elephant2 ...);
```

### Retrieve documents from the database
Get a single document from the database:
```cs
//Get first document in collection
var elephant1 = animals.First();

//Get first document matching filter
var elephant2 = animals.First(x => x.Name == "Elephant");

//You can also use the mongoDB driver filter syntax:
var filter = Builders<Animal>.Filter.Eq("_id", id);
var elephant3 = animals.First(filter);

//Also possible: use JSON to filter
var elephant4 = animals.First("{ Name: 'Elephant' }");

//Or even an object or BSONDocument!
var elephant5 = animals.First(new { Name = "Elephant" });
var elephant6 = animals.First(new BsonDocument("Name","Elephant"));
```

Get multiple documents from the database:
```cs
//Same syntax as .First(), but a filter is required.
var elephants = animals.Find(x => x.Name == "Elephant");

//To get first 5 animals with LINQ:
var firstFive = animals.Take(5).ToList();

//Also supports the other filter methods: BSON, JSON, object and MongoDB driver Filter.
```

Get document by ID:
```cs
var elephant = animals.FindById("608c09348a21d1fc12ffafc8");
```

### Count documents
To count all documents, use the following syntax:
```cs
long count = animals.Count();

//You can also use filters in the count function.
//It supports all filter types used in First() like JSON:
long elephantCount = animals.Count("{ Name: 'Elephant' }");
```

### Update documents
To update and replace a single document, take an instance and modify some fields.
```cs
var any = animals.First();
any.Name = "Tiger";

//The update function returns the updated instance
any = animals.Update(any);
```

To update only one field, use the following syntax from the Mongo driver:
```cs
var any = animals.First();
var update = Builders<Animal>.Update.Set("Name", "Tiger");

bool result = animals.Update(any.Id, update);
//If true, update succeed
```
You can also use the other syntaxis, like BSON, JSON or object syntax .

To update multiple documents, use the mongo driver filter syntax in combination with the update syntax:
```cs
//Update using the mongoDB update syntax
var update = Builders<Animal>.Update.Set("Name", "Tiger");

//Filter using the mongoDB filter syntax
var filter = Builders<Animal>.Filter.Eq("Name","Elephant");

var result = animals.UpdateMany(filter, update);
```

### Delete documents
To delete a single document, you can use the following methods:
```cs
//Delete by object
Animal first = animals.First();

bool deleted = animals.Delete(first);
//Or
bool deleted = animals.Delete(first.Id); //delete by ID
```

To delete many documents use a filter:
```cs
int deletedCnt = animals.DeleteMany(x => x.Name == "Tiger");

//You can also use JSON, BSON or object syntax here. Or use the MongoDB filter syntax
```

### Advanced CRUD operations
To do more advanced CRUD operations you might need to use the MongoDB driver syntax. The collection can easily be accessed via the repository.
```cs
//To get the IMongoCollection call the .Collection property
var collection = animals.Collection;

//Now you can do more advanced things like creating an aggregate:
var aggregate = collection.Aggregate().Match(x => x.Name == "Elephant").Limit(1);
var result = aggregate.First();
```

## Relations
Everything done in the previous chapters isn't that difficult to do with the MongoDB Driver only. But when using references, it is getting more advanced. Let's take a look what is needed to populate a collection by hand.

```cs
// !!! WARNING !!! this is just an example do NOT populate your attributes in this way!
var caretakers = database.GetCollection<Person>("persons");

var animal = animals.First();
var caretaker = caretakers.Find(x => x.Id == animal.CaretakerRef).First();
animal.Caretaker = caretaker;
```
Like you see, thats a lot of effort for just a one animal. Imagine that you have a nested reference (like a list of references with each in it)
```cs
//Example of more complex structure:
class User : Entity {
    public string Name { get; set; }

    [Reference("orders", "OrderRefs")]
    public List<Order> Orders { get; set; }
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> OrderRefs { get; set; }
}

class Product : Entity {
    public string Name { get; set; }
    public decimal Price { get; set; }
}

class Order : Entity {
    public DateTime Timestamp { get; set;}

    [Reference("products", "ProductRefs")]
    public List<Product> Products { get; set; }
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> ProductRefs { get; set; }
}
```
I wish you good luck with writing your query to get one user including its orders and products!
This is why we created MongoODM. To automate lookup en populate actions. It is very easy to populate the orders and products on-the-fly using the populate functionality.

```cs
IMongoRepository<User> users = new MongoRepository<User>(database, "users");

//Get first user with everything populated:
users.Populate("Orders", "Orders.Products");
var user = users.First();
```
That's all you have to do. Much times easier, isn't it?

## Populate attributes
### Reference attribute
To populate single or collections of reference documents you have to use the ReferenceAttribute. It looks like follows:
```cs
ReferenceAttribute(string refCollection, string localField, string refField = "_id", bool autoPopulate = false)
```

You already saw a few examples. The first field in the constructor is the referenced collection (like "orders"), the second paramter is the local field containing the reference(s). The third field is the name of the indentifier field in the other collection, default _id. The fourth field is a boolean to enable/disable automatic population.

If you want to automatically populate, use the attribute like this:
```cs
public class User : Entity {
    [Reference("orders", "OrderRefs", autoPopulate: true)]
    public List<Order> Orders { get; set; }
    [BsonRepresentation(BsonType.ObjectId)]
    public List<string> OrderRefs { get; set; }
}
```

When you now call .First or .Find, the Orders are automatically queried from the orders table. 

### Embed attribute
The second attribute is the EmbedAttribute. This attribute is used to decorate a propery that is embedded instead of referenced to tell MongoODM that it needs to look if there are references within the embedded object. This is used like this:
```cs
public class Address { //Notice that embed doesnt need an Id, and no need to implement Entity
    public string Street { get; set; }
    public int HouseNumber { get; set; }

    //The nested reference
    [Reference("cities", "CityRef", autoPopulate: true)]
    public City City { get; set; }
    [BsonRepresentation(BsonType.ObjectId)]
    public string CityRef { get; set; }
}

public class User : Entity {
    public string Name { get; set; }

    [Embed] //<-- mark with embed to enable population of nested elements.
    public Address Address { get; set; }
}
```

### Depopulate
If you are using autoPopulate and you want to disable it during runtime, use the .Depopulate() function
```cs
//I want to disable the population of the City
users.Depopulate("Address.City");

//I want to enable it again
users.Populate("Address.City");
users.ResetPopulates(); //Resets all populate/depopulates
```

## Repository
The repository has a few tweaks and features that we haven't covered yet. The class provides a few settings and special methods to make interacting with the database easier.

### Repository settings
```cs
bool UseLogging = false; // Enable/disable logging. Defaults to false
bool UseLookup = false; //When enabled the ODM uses the $lookup operator, when disabled it uses the $in operator to do all lookups. Defaults to false
bool DoPopulate = true; //When disabled no population is done at all, even when .Populate is called. Default false
bool UseDepopulate = false; //Beta feature. Depopulates object on update/insert, defaults to false
```
When UseDepopulate is set to true, it maps objects back to references. So if you populate City and change it into another City (with another id) and update the model, the Id of the City object is taken and set to the CityRef. If UseDepopulate is disabled the change is ignored and only manually changes to the CityRef are saved. In future this will default to true but it is now unstable.
You can change the settings by changing the properies of your instance:
```cs
animals.DoPopulate = false; //change setting of repository instance
```

### Other repository functions
You've seen the special functions Populate, Depopulate and ResetPopulates. Other helper functions are:
```cs
FilterDefinition<T> GetFilter(string id) //returns filter with Eq("_id", id) from an id.
```
All CRUD-related functions can be found [here](#crud-functionality).
All populate-related functions are described [here](#depopulate).