using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace WebTicketBackEnd.Models
{
    public static class ModelsExtensionMethods
    {
        public static UserModel getUserModel(this UserApiModel userApiModel, String Id)
        {
            UserModel userModel = new UserModel
            {
                Id = new ObjectId(Id),
                Email = userApiModel.Email.ToLower(),
                Password = userApiModel.Password,
                Birthday = DateTime.ParseExact(userApiModel.Birthday, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                Name = userApiModel.Name,
                Surname = userApiModel.Surname,
                RegisteredEvents = new List<ObjectId>()
            };

            return userModel;
        }

        public static UserApiModel getUserApiModel(this UserModel userModel)
        {
            UserApiModel userApiModel = new UserApiModel
            {
                Email = userModel.Email.ToLower(),
                Password = userModel.Password,
                Birthday = String.Format("{0:yyyy-MM-dd HH:mm}", userModel.Birthday),
                Name = userModel.Name,
                Surname = userModel.Surname

            };

            return userApiModel;
        }

        public static EventModel getEventModel(this EventApiModel eventApiModel, String CreatorId, DateTime dateCreated)
        {
            EventModel eventModel = new EventModel
            {
                Id = new ObjectId(),
                Name = eventApiModel.Name,
                StartDate = DateTime.ParseExact(eventApiModel.StartDate, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                EndDate = DateTime.ParseExact(eventApiModel.EndDate, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                DateCreated = dateCreated,
                Location = eventApiModel.Location,
                Description = eventApiModel.Description,
                Url = eventApiModel.Url,
                Image = eventApiModel.Image,
                CreatorId = new ObjectId(CreatorId)
            };

            return eventModel;
        }

        public static EventApiModel getEventApiModel(this EventModel eventModel)
        {
            EventApiModel eventApiModel = new EventApiModel
            {
                Id = eventModel.Id.ToString(),
                Name = eventModel.Name,
                StartDate = String.Format("{0:yyyy-MM-dd HH:mm}", eventModel.StartDate),
                EndDate = String.Format("{0:yyyy-MM-dd HH:mm}", eventModel.EndDate),
                Location = eventModel.Location,
                Description = eventModel.Description,
                Url = eventModel.Url,
                Image = eventModel.Image
            };

            return eventApiModel;
        }

        public static ReviewModel getReviewModel(this ReviewApiModel reviewApiModel, String Id, String UserId, String EventId)
        {
            ReviewModel reviewModel = new ReviewModel
            {
                Id = new ObjectId(Id),
                Rating = reviewApiModel.Rating,
                Opinion = reviewApiModel.Opinion,
                LastEdit = DateTime.ParseExact(reviewApiModel.LastEdit, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                UserId = new ObjectId(UserId),
                EventId = new ObjectId(EventId)
            };

            return reviewModel;
        }

        public static ReviewApiModel getReviewApiModel(this ReviewModel reviewModel, String userName)
        {
            ReviewApiModel reviewApiModel = new ReviewApiModel
            {
                Rating = (int)reviewModel.Rating,
                Opinion = reviewModel.Opinion,
                LastEdit = String.Format("{0:yyyy-MM-dd HH:mm}", reviewModel.LastEdit),
                UserName = userName
            };

            return reviewApiModel;
        }

        public static MessageApiModel getMessageApiModel(this MessageModel messageModel, string userName = "")
        {
            MessageApiModel messagesApiModel = new MessageApiModel
            {
                Message = messageModel.Message,
                DateSent = String.Format("{0:yyyy-MM-dd HH:mm}", messageModel.DateSent),
                userName = userName
            };

            return messagesApiModel;
        }

        public const String zeroId = "000000000000000000000000";
    }
}
