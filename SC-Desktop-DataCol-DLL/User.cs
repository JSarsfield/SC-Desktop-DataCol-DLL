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
    /// Class representation of a MongoDB BSON User document that sits in Users collection
    /// </summary>
    [BsonIgnoreExtraElements]  // Ignore any extra elements from the BSON document
    class User
    {
        [BsonRepresentation(BsonType.ObjectId)]  // BsonRepresentation used when you need to dictate the type representation between the C# type and BSON type.
        public string Id { get; set; }

        [BsonElement("username")]
        public string Username { get; set; }

        [BsonElement("optin")]
        public bool Optin { get; set; }
    }
}