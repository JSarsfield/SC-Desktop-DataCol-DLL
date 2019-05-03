using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC_Desktop_DataCol_DLL
{
    /// <summary>
    /// Class representation of a MongoDB BSON Session document that sits in Sessions collection
    /// </summary>
    [BsonIgnoreExtraElements]  // Ignore any extra elements from the BSON document
    class Session
    {
        [BsonRepresentation(BsonType.ObjectId)]  // BsonRepresentation used when you need to dictate the type representation between the C# type and BSON type.
        public string Id { get; set; }

        [BsonRepresentation(BsonType.String)]
        public string username { get; set; }

        [BsonRepresentation(BsonType.String)]
        public string computer { get; set; }

        [BsonRepresentation(BsonType.String)]
        public string date { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)] // http://mongodb.github.io/mongo-csharp-driver/2.5/reference/bson/mapping/
        public DateTime startTime { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime lastActivity { get; set; }
        //[BsonExtraElements]
        //public BsonDocument CatchAll { get; set; }  // Catch elements from the BSON document that doesn't have a field.
        //BlockingCollection<Data> dataItems = new BlockingCollection<Data>(100);

        public Session(string _username)
        {
            username = _username;
            computer = Environment.MachineName;
            startTime = DateTime.UtcNow;
            lastActivity = startTime;
            date = startTime.ToString("yyyy-MM-dd");
            // Create Session document
        }

        public void SendUpdateToDB()
        {
            // Lock activity object
            // Create filter here with Builders for updating fields
        }

        public void AddActivity()
        {
            // Lock activity object
        }
    }
}
