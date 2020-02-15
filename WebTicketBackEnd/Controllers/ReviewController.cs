using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using WebTicketBackEnd.Models;
using WebTicketBackEnd.Utils;

namespace WebTicketBackEnd.Controllers
{
    [Route("api/review")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        [HttpGet("browse/{eventId}/{pageSize}/{pageId}")]
        public IActionResult GetReview([FromRoute] string eventId, [FromRoute] int pageSize, [FromRoute] int pageId)
        {
            List<ReviewModel> reviews = MongoUtil.GetReviews(new ObjectId(eventId), pageSize, pageId);
            List<ReviewApiModel> apiReviews = reviews.ConvertAll(new Converter<ReviewModel, ReviewApiModel>(r => {
                UserModel user = MongoUtil.GetUser(r.UserId);
                ReviewApiModel apiR = r.getReviewApiModel(user.Name);
                return apiR;
            }));

            return Ok(apiReviews);
        }

        [HttpGet("get/{eventId}"), Authorize]
        public IActionResult GetReview([FromRoute] string eventId, [FromHeader(Name = "Authorization")] string userToken)
        {
            ObjectId userId = new ObjectId(JwtUtil.GetUserIdFromToken(userToken));
            ReviewModel review = MongoUtil.GetReview(userId, new ObjectId(eventId));
            UserModel user = MongoUtil.GetUser(userId);

            if (review != null)
            {
                return Ok(review.getReviewApiModel(user.Name));
            }

            return Ok(new ReviewModel { Rating = 0, Opinion = "" });
        }
        [HttpPut("edit/{eventId}")]
        public IActionResult EditReview([FromRoute] String eventId, [FromHeader(Name = "Authorization")] String userToken, [FromBody] ReviewApiModel reviewApiModel)
        {
            MongoUtil.EditReview(new ObjectId(JwtUtil.GetUserIdFromToken(userToken)), 
                                new ObjectId(eventId), 
                                reviewApiModel.Rating, 
                                reviewApiModel.Opinion, 
                                DateTime.Now);

            return Ok();
        }

        [HttpDelete("delete/{eventId}"), Authorize]
        public IActionResult DeleteReview([FromRoute] String eventId, [FromHeader(Name = "Authorization")] String userToken)
        {
            MongoUtil.DeleteReview(new ObjectId(JwtUtil.GetUserIdFromToken(userToken)), 
                                new ObjectId(eventId));

            return Ok();
        }
    }
}
