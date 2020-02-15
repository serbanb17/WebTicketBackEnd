using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using WebTicketBackEnd.Models;
using WebTicketBackEnd.Utils;

namespace WebTicketBackEnd.Controllers
{
    [Route("api/event")]
    [ApiController]
    public class EventController : ControllerBase
    {
        [HttpGet("pie-chart/{eventId}"), Authorize]
        public IActionResult GetPieChartData([FromHeader(Name = "Authorization")] string token, [FromRoute] string eventId)
        {
            string userId = JwtUtil.GetUserIdFromToken(token);
            EventModel ev = MongoUtil.GetEvent(new ObjectId(eventId));

            if (ev.CreatorId == new ObjectId(userId))
                return Ok(MongoUtil.countRegistrations(new ObjectId(eventId)));

            return Unauthorized();
        }

        [HttpGet("line-chart/{eventId}"), Authorize]
        public IActionResult GetLineChartData([FromHeader(Name = "Authorization")] string token, [FromRoute] string eventId)
        {
            string userId = JwtUtil.GetUserIdFromToken(token);
            EventModel ev = MongoUtil.GetEvent(new ObjectId(eventId));

            if (ev.CreatorId == new ObjectId(userId))
                return Ok(MongoUtil.countMsgs(new ObjectId(eventId)));

            return Unauthorized();
        }

        [HttpGet("reviews-stats/{eventId}"), Authorize]
        public IActionResult GetReviewsStats([FromHeader(Name = "Authorization")] string token, [FromRoute] string eventId)
        {
            string userId = JwtUtil.GetUserIdFromToken(token);
            EventModel ev = MongoUtil.GetEvent(new ObjectId(eventId));

            if(ev.CreatorId == new ObjectId(userId))
                return Ok(MongoUtil.getReviewsStats(new ObjectId(eventId)));

            return Unauthorized();
        }

        [HttpGet("recommendations/{pageSize}/{pageId}"), Authorize]
        public IActionResult GetRecommendations([FromHeader(Name = "Authorization")] string token, [FromRoute] int pageSize, [FromRoute] int pageId)
        {
            string userId = JwtUtil.GetUserIdFromToken(token);
            string filename = @"eventRecommenderTestedOnMovieLensSmallDataSet\predictions\" + userId.ToString() + ".txt";

            if (!System.IO.File.Exists(filename))
            {
                Process cmd = new Process();
                cmd.StartInfo.FileName = "cmd.exe";
                cmd.StartInfo.Arguments = "/C cd eventRecommenderTestedOnMovieLensSmallDataSet & Python35\\python.exe predict.py " + userId;
                cmd.Start();
                cmd.WaitForExit();
            }

            List<EventApiModel> apiEvents = new List<EventApiModel>();

            if (System.IO.File.Exists(filename))
            {
                using (StreamReader reader = new StreamReader(filename))
                {
                    for (int i = 0; i < pageSize * pageId && !reader.EndOfStream; i++)
                        reader.ReadLine();

                    for (int i = 0; i < pageSize && !reader.EndOfStream; i++)
                    {
                        string line = reader.ReadLine();
                        string[] values = line.Split(',');
                        string eventIdStr = values[0];
                        ObjectId eventId = new ObjectId(eventIdStr);
                        EventModel em = MongoUtil.GetEvent(eventId);
                        apiEvents.Add(em.getEventApiModel());
                    }
                }
            }

            return Ok(apiEvents);
        }

        [HttpPost("create"), Authorize]
        public IActionResult Create([FromHeader(Name = "Authorization")] string token, [FromBody] EventApiModel eventApiModel)
        {
            String userId = JwtUtil.GetUserIdFromToken(token);
            EventModel eventModel = eventApiModel.getEventModel(userId, DateTime.Now);
            eventModel.Image = "StaticFiles/Images/standard.jpg";
            MongoUtil.AddEvent(eventModel);

            return Ok("Event created");
        }

        [HttpPost("upload-image"), DisableRequestSizeLimit, Authorize]
        public IActionResult Upload([FromHeader(Name = "Authorization")] string token)
        {
            try
            {
                String userId = JwtUtil.GetUserIdFromToken(token);
                var file = Request.Form.Files[0];
                var folderName = Path.Combine("StaticFiles", "Images");
                var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);

                if (file.Length > 0)
                {
                    var fileName = userId + "_" + DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString() + ".jpg";
                    var fullPath = Path.Combine(pathToSave, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    List<EventModel> eventList = MongoUtil.GetCreatedEvents(new ObjectId(userId), 1, 0);
                    if (eventList.Count > 0 && eventList[0].Image == "StaticFiles/Images/standard.jpg")
                    {
                        string newImage = "StaticFiles/Images/" + fileName;
                        MongoUtil.UpdateImage(eventList[0].Id, newImage);
                    }


                    return Ok("Image updated");
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("details/{eventId}")]
        public IActionResult Details([FromRoute] string eventId)
        {
            EventModel eventModel = MongoUtil.GetEvent(new ObjectId(eventId));
            return Ok(eventModel.getEventApiModel());
        }

        [HttpGet("browse/{pageSize}/{pageId}")]
        public IActionResult Browse([FromRoute] int pageSize, [FromRoute] int pageId)
        {
            return Ok(MongoUtil.GetEvents(pageSize, pageId)
                .ConvertAll(new Converter<EventModel, EventApiModel>(eventModel => {
                    return eventModel.getEventApiModel();
                })));
        }

        [HttpGet("search/{pageSize}/{pageId}/{searchText}")]
        public IActionResult Search([FromRoute] int pageSize, [FromRoute] int pageId, [FromRoute] String searchText)
        {   
            return Ok(MongoUtil.Search(pageSize, pageId, searchText)
                .ConvertAll(new Converter<EventModel, EventApiModel>(eventModel => {
                    return eventModel.getEventApiModel();
                })));
        }

        [HttpGet("chat-messages/{eventId}")]
        public IActionResult GetMessages([FromRoute] String eventId, [FromHeader(Name = "Authorization")] string token)
        {
            ObjectId reqUserId = new ObjectId(JwtUtil.GetUserIdFromToken(token));
            List<MessageModel> messages = MongoUtil.GetMessages(new ObjectId(eventId));
            List<MessageApiModel> apiMessages = messages.ConvertAll(new Converter<MessageModel, MessageApiModel>(msg => {
                ObjectId pubUserId = msg.UserId;
                if (pubUserId == reqUserId)
                    return msg.getMessageApiModel();
                msg.DateSent = msg.DateSent.AddHours(3);
                return msg.getMessageApiModel(MongoUtil.GetUser(pubUserId).Name);
            }));

            return Ok(apiMessages);
        }
    }
}