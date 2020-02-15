using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebTicketBackEnd.Models;

namespace WebTicketBackEnd.Utils
{
    public class MongoUtil
    {
        #region User Util

        public static UserModel GetUser(ObjectId userId)
        {
            return _userColl.Find(u => u.Id == userId).FirstOrDefault();
        }

        public static UserModel GetUser(string email)
        {
            return _userColl.Find(u => u.Email.ToLower() == email.ToLower()).FirstOrDefault();
        }

        public static UserModel GetUser(string email, string password)
        {
            return _userColl.Find(u => u.Email.ToLower() == email.ToLower() && u.Password == password).FirstOrDefault();
        }

        public static void AddUser(UserModel userModel)
        {
            _userColl.InsertOne(userModel);
        }

        public static void ChangePassword(ObjectId userId, string newPass)
        {
            _userColl.FindOneAndUpdate(Builders<UserModel>.Filter.Eq("Id", userId), Builders<UserModel>.Update.Set("Password", newPass));
        }

        public static List<EventModel> GetRegisteredEvents(ObjectId userId, int pageSize, int pageId)
        {
            UserModel user = GetUser(userId);

            return _eventColl.Find(e => user.RegisteredEvents.Contains(e.Id)).Skip(pageId * pageSize).Limit(pageSize).ToList();
        }
        
        public static List<ReviewModel> GetUserReviews(ObjectId userId, int pageSize, int pageId)
        {
            return _reviewColl.Find(r => r.UserId == userId).Skip(pageId * pageSize).Limit(pageSize).ToList();
        }

        public static List<EventModel> GetCreatedEvents(ObjectId userId, int pageSize, int pageId)
        {
            List<EventModel> resp = _eventColl.Find(e => e.CreatorId == userId).SortByDescending(e => e.DateCreated).Skip(pageId * pageSize).Limit(pageSize).ToList();
            return resp;
        }
        
        public static String GetRegistrationStatus(ObjectId eventId, ObjectId userId)
        {
            String status = "server problem";
            UserModel user = GetUser(userId);
            EventModel ev = GetEvent(eventId);

            if(ev.CreatorId == user.Id)
            {
                status = "creator";
            }
            else if(user.RegisteredEvents.Contains(ev.Id))
            {
                status = "registered";
            }
            else
            {
                status = "unregistered";
            }

            return status;
        }

        public static Boolean RegisterUserToEvent(ObjectId eventId, ObjectId userId)
        {
            String status = GetRegistrationStatus(eventId, userId);
            if (status == "unregistered")
            {
                _userColl.FindOneAndUpdate(Builders<UserModel>.Filter.Eq("Id", userId), Builders<UserModel>.Update.Push("RegisteredEvents", eventId));
                return true;
            }
            return false;
        }
        
        public static Boolean UnregisterUserFromEvent(ObjectId eventId, ObjectId userId)
        {
            String status = GetRegistrationStatus(eventId, userId);
            if (status == "registered")
            {
                _userColl.FindOneAndUpdate(Builders<UserModel>.Filter.Eq("Id", userId), Builders<UserModel>.Update.Pull("RegisteredEvents", eventId));
                return true;
            }
            return false;
        }

        #endregion

        #region Event Util

        public static EventModel GetEvent(ObjectId eventId)
        {
            return _eventColl.Find(e => e.Id == eventId).FirstOrDefault();
        }
        
        public static List<EventModel> GetEvents(int pageSize, int pageId)
        {
            return _eventColl.Find(e => true).Skip(pageId * pageSize).Limit(pageSize).ToList();
        }

        public static void AddEvent(EventModel eventModel)
        {
            _eventColl.InsertOne(eventModel);
        }

        public static void UpdateImage(ObjectId eventId, string image)
        {
            var filter = Builders<EventModel>.Filter.Eq(e => e.Id, eventId);
            var update = Builders<EventModel>.Update.Set(e => e.Image, image);
            _eventColl.UpdateOne(filter, update);
        }

        public static List<EventModel> Search(int pageSize, int pageId, String searchText)
        {
            FilterDefinition<EventModel> filter = Builders<EventModel>.Filter.Text(searchText);
            ProjectionDefinition<EventModel> projection = Builders<EventModel>.Projection.MetaTextScore("TextMatchScore");
            SortDefinition<EventModel> sort = Builders<EventModel>.Sort.MetaTextScore("TextMatchScore");

            return _eventColl.Find(filter).Project(projection).Sort(sort).Skip(pageId * pageSize).Limit(pageSize)
                .ToList().ConvertAll(new Converter<BsonDocument, EventModel>(b => {
                    b.Remove("TextMatchScore");
                    return new EventModel
                    {
                        Id = b["_id"].AsObjectId,
                        Name = b["name"].AsString,
                        StartDate = b["startDate"].ToUniversalTime(),
                        EndDate = b["endDate"].ToUniversalTime(),
                        Location = b["location"].AsString,
                        Description = b["description"].AsString,
                        Url = b["url"].AsString,
                        Image = b["image"].AsString,
                        CreatorId = b["creatorId"].AsObjectId
                    };
                }));
        }
        
        public static List<long> countRegistrations(ObjectId eventId)
        {
            EventModel ev = GetEvent(eventId);
            ObjectId creatorId = ev.CreatorId;
            List<ObjectId> otherEvents = _eventColl.Find(e => e.CreatorId == creatorId && e.Id != eventId).Project(e => e.Id).ToList();
            List<UserModel> allUsers = _userColl.Find(u => true).ToList();
            
            long subscribedToThisEvent = allUsers.Where(u => u.RegisteredEvents != null && u.RegisteredEvents.Contains(eventId) && !u.RegisteredEvents.Intersect(otherEvents).Any()).Count();
            long subscribedToOthers = allUsers.Where(u => u.RegisteredEvents != null && !u.RegisteredEvents.Contains(eventId) && u.RegisteredEvents.Intersect(otherEvents).Any()).Count();
            long subscribedBoth = allUsers.Where(u => u.RegisteredEvents != null && u.RegisteredEvents.Contains(eventId) && u.RegisteredEvents.Intersect(otherEvents).Any()).Count();
            long total = allUsers.Where(u => u.RegisteredEvents != null && (u.RegisteredEvents.Contains(eventId) || u.RegisteredEvents.Intersect(otherEvents).Any())).Count();

            return new List<long> { subscribedToThisEvent, subscribedToOthers, subscribedBoth };
        }

        public static Tuple<List<long>, List<string>> countMsgs(ObjectId eventId)
        {
            List<long> resp = new List<long>();
            List<string> labels = new List<string>();
            EventModel ev = GetEvent(eventId);
            DateTime d1 = ev.DateCreated.AddDays(-ev.DateCreated.Day + 1);
            DateTime d2 = DateTime.Now;
            List<MessageModel> msgs = _messageColl.Find(m => m.EventId == eventId).ToList();

            for(DateTime d = d1; d.Date < d2.Date; d = d.AddMonths(1))
            {
                long x = msgs.Where(m => m.DateSent.Year == d.Year && m.DateSent.Month == d.Month).Count();
                resp.Add(x);
            }

            labels.Add(String.Format("{0:dd-MM-yy}", d1));
            labels.Add(String.Format("{0:dd-MM-yy}", d2));
            labels.Add(msgs.Count.ToString());

            return new Tuple<List<long>, List<string>>(resp, labels);
        }

        public static Tuple<long, float> getReviewsStats(ObjectId eventId)
        {
            return new Tuple<long, float>
            (
                _reviewColl.CountDocuments(r => r.EventId == eventId),
                _reviewColl.AsQueryable().Where(r => r.EventId == eventId).ToList().Average(r => r.Rating)
            );
        }

        #endregion

        #region Review Util

        public static List<ReviewModel> GetReviews(ObjectId eventId, int pageSize, int pageId)
        {
            return _reviewColl.Find(r => r.EventId == eventId).Sort(Builders<ReviewModel>.Sort.Descending(r => r.LastEdit)).Skip(pageId * pageSize).Limit(pageSize).ToList();
        }

        public static ReviewModel GetReview(ObjectId userId, ObjectId eventId)
        {
            return _reviewColl.Find(r => r.UserId == userId && r.EventId == eventId).FirstOrDefault();
        }

        public static void EditReview(ObjectId userId, ObjectId eventId, int rating, String opinion, DateTime lastEdit)
        {
            if(_reviewColl.Find(r => r.UserId == userId && r.EventId == eventId).CountDocuments() == 0)
            {
                ReviewModel reviewModel = new ReviewModel
                {
                    Id = new ObjectId(),
                    Rating = rating,
                    Opinion = opinion,
                    LastEdit = lastEdit,
                    UserId = userId,
                    EventId = eventId
                };

                _reviewColl.InsertOne(reviewModel);
            }
            else
            {
                _reviewColl.UpdateOne(r => r.UserId == userId && r.EventId == eventId,
                                    Builders<ReviewModel>.Update.Set(r => r.Rating, rating)
                                                                .Set(r => r.Opinion, opinion)
                                                                .Set(r => r.LastEdit, lastEdit));
            }
        }

        public static void DeleteReview(ObjectId userId, ObjectId eventId)
        {
            _reviewColl.DeleteOne(r => r.UserId == userId && r.EventId == eventId);
        }

        public static List<EventModel> getRecommendations()
        {
            long totalUsers = _userColl.CountDocuments(u => true);
            long totalEvents = _eventColl.CountDocuments(e => true);

            using (StreamWriter file = new StreamWriter("AllReviewsMatrix.txt"))
            {
               file.WriteLine(totalUsers + " " + totalEvents);

                for (int i = 0; i < totalUsers; i++)
                {
                    UserModel user = _userColl.Find(u => true).Skip(i).Limit(1).FirstOrDefault();
                    for (int j = 0; j < totalEvents; j++)
                    {
                        EventModel evnt = _eventColl.Find(u => true).Skip(j).Limit(1).FirstOrDefault();
                        ReviewModel review = GetReview(user.Id, evnt.Id);
                        if (review != null)
                        {
                            file.Write(review.Rating + " ");
                        }
                        else
                        {
                            file.Write("0 ");
                        }
                    }
                    file.WriteLine();
                }
            }

            return null;
        }

        #endregion

        #region ChatRoom Util
        public static List<MessageModel> GetMessages(ObjectId eventId)
        {
            return _messageColl.Find(m => m.EventId == eventId).SortBy(m => m.DateSent).ToList();
        }

        public static void SaveMessage(MessageModel messageModel)
        {
            _messageColl.InsertOne(messageModel);
        }
        #endregion

        public static void InitializeConnection(String connectionString, String databaseName)
        {
            _conn = new MongoClient(connectionString);
            _db = _conn.GetDatabase(databaseName);
            _userColl = _db.GetCollection<UserModel>("user");
            _eventColl = _db.GetCollection<EventModel>("event");
            _reviewColl = _db.GetCollection<ReviewModel>("review");
            _messageColl = _db.GetCollection<MessageModel>("message");
        }

        private static MongoClient _conn;
        private static IMongoDatabase _db;
        private static IMongoCollection<UserModel> _userColl;
        private static IMongoCollection<EventModel> _eventColl;
        private static IMongoCollection<ReviewModel> _reviewColl;
        private static IMongoCollection<MessageModel> _messageColl;
    }
}
