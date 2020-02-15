using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using WebTicketBackEnd.Models;
using WebTicketBackEnd.Utils;


namespace WebTicketBackEnd.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        #region User Entity

        [HttpPost("signup")]
        public IActionResult Signup([FromBody] UserApiModel userApiModel)
        {
            if (MongoUtil.GetUser(userApiModel.getUserModel(ModelsExtensionMethods.zeroId).Email) == null)
            {
                MongoUtil.AddUser(userApiModel.getUserModel(ModelsExtensionMethods.zeroId));
                return Ok("Success");
            }

            return Conflict("Email used");
        }

        [HttpPost("signin")]
        public IActionResult Signin([FromBody] UserApiModel userApiModel)
        {
            UserModel user = MongoUtil.GetUser(userApiModel.Email, userApiModel.Password);

            if (user != null)
            {
                string token = JwtUtil.getToken(user.Id.ToString(), user.Email, DateTime.Now.AddMinutes(30));
                return Ok(token);
            }

            return BadRequest("Invalid email or password!");
        }

        [HttpGet("who-i-am"),Authorize]
        public IActionResult WhoIAm([FromHeader(Name = "Authorization")] string token)
        {
            ObjectId userId = new ObjectId(JwtUtil.GetUserIdFromToken(token));
            UserModel um = MongoUtil.GetUser(userId);
            UserApiModel uam = um.getUserApiModel();
            uam.Password = "";
            uam.Birthday = "";

            return Ok(uam);
        }

        [HttpGet("send-password/{email}")]
        public IActionResult SendPasswordThroughEmail([FromRoute] string email)
        {
            UserModel user = MongoUtil.GetUser(email);

            if (user != null)
            {
                var smtpClient = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    Credentials = new NetworkCredential(Environment.GetEnvironmentVariable("SmtpUserName"), Environment.GetEnvironmentVariable("SmtpPassword"))
                };

                using (var message = new MailMessage(new MailAddress(Environment.GetEnvironmentVariable("SmtpUserName"), "WebTicket"), new MailAddress(user.Email))
                {
                    Subject = "Parolă cont",
                    Body = "Salut, " + user.Surname + " " + user.Name + ",\n\n" + Environment.NewLine + Environment.NewLine + "Parola contului tău este " + user.Password + Environment.NewLine + Environment.NewLine + "Nu răspunde acestei adrese de email. Este folosită doar pentru mesaje automate!"
                })
                {
                    smtpClient.Send(message);
                }

                return Ok("Parola a fost trimisă pe email");
            }

            return Ok("Adresa de email introdusă nu se regăsește în baza de date!");
        }

        [HttpPatch("change-password"), Authorize]
        public IActionResult ChangePassword([FromBody] UserApiModel newPass, [FromHeader(Name = "Authorization")] string token)
        {
            ObjectId userId = new ObjectId(JwtUtil.GetUserIdFromToken(token));

            MongoUtil.ChangePassword(userId, newPass.Password);

            return Ok();
        }

        #endregion

        #region User Events Info

        [HttpGet("registered-events/{pageSize}/{pageId}"), Authorize]
        public IActionResult Registered([FromRoute] int pageSize, [FromRoute] int pageId, [FromHeader(Name = "Authorization")] string token)
        {
            ObjectId userId = new ObjectId(JwtUtil.GetUserIdFromToken(token));
            return Ok(MongoUtil.GetRegisteredEvents(userId, pageSize, pageId)
                .ConvertAll(new Converter<EventModel, EventApiModel>(e => {
                    return e.getEventApiModel();
                })));
        }

        [HttpGet("reviewed-events/{pageSize}/{pageId}"), Authorize]
        public IActionResult Reviewed([FromRoute] int pageSize, [FromRoute] int pageId, [FromHeader(Name = "Authorization")] string token)
        {
            ObjectId userId = new ObjectId(JwtUtil.GetUserIdFromToken(token));
            List<ReviewModel> reviews = MongoUtil.GetUserReviews(userId, pageSize, pageId);
            List<EventApiModel> events = new List<EventApiModel>();

            foreach (ReviewModel review in reviews)
            {
                events.Add(MongoUtil.GetEvent(review.EventId).getEventApiModel());
            }

            return Ok(events);
        }

        [HttpGet("created-events/{pageSize}/{pageId}"), Authorize]
        public IActionResult Created([FromRoute] int pageSize, [FromRoute] int pageId, [FromHeader(Name = "Authorization")] string token)
        {
            ObjectId userId = new ObjectId(JwtUtil.GetUserIdFromToken(token));
            return Ok(MongoUtil.GetCreatedEvents(userId, pageSize, pageId)
                .ConvertAll(new Converter<EventModel, EventApiModel>(e => {
                    return e.getEventApiModel();
                })));
        }

        #endregion

        #region User Event Status

        [HttpGet("registration-status/{eventId}"), Authorize]
        public IActionResult RegistrationStatus([FromRoute] string eventId, [FromHeader(Name = "Authorization")] string userToken)
        {
            ObjectId userId = new ObjectId(JwtUtil.GetUserIdFromToken(userToken));
            String status = MongoUtil.GetRegistrationStatus(new ObjectId(eventId), userId);
            return Ok(status);
        }

        [HttpPatch("register/{eventId}"), Authorize]
        public IActionResult RegisterUserToEvent([FromRoute] string eventId, [FromHeader(Name = "Authorization")] string token)
        {
            ObjectId userId = new ObjectId(JwtUtil.GetUserIdFromToken(token));
            Boolean ok = MongoUtil.RegisterUserToEvent(new ObjectId(eventId), userId);
            return Ok(ok);
        }

        [HttpPatch("unregister/{eventId}"), Authorize]
        public IActionResult UnregisterUserToEvent([FromRoute] string eventId, [FromHeader(Name = "Authorization")] string token)
        {
            ObjectId userId = new ObjectId(JwtUtil.GetUserIdFromToken(token));
            Boolean ok = MongoUtil.UnregisterUserFromEvent(new ObjectId(eventId), userId);
            MongoUtil.DeleteReview(userId, new ObjectId(eventId));
            return Ok(ok);
        }

        #endregion
    }
}