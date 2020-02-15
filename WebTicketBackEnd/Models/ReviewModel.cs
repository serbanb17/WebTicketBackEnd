using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebTicketBackEnd.Models
{
    public class ReviewModel
    {
        [BsonId]
        public ObjectId Id { get; set; }
        [BsonElement("rating")]
        public float Rating { get; set; }
        [BsonElement("opinion")]
        public string Opinion { get; set; }
        [BsonElement("lastEdit")]
        public DateTime LastEdit { get; set; }
        [BsonElement("userId")]
        public ObjectId UserId { get; set; }
        [BsonElement("eventId")]
        public ObjectId EventId { get; set; }
    }
}
