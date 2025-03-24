using Microsoft.AspNetCore.Mvc;
using Converse.Services.Message;
using Converse.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Converse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly MessageService _messageService;

        public MessageController(MessageService messageService)
        {
            _messageService = messageService;
        }

        [HttpPost("history")]
        public async Task<IActionResult> GetMessageHistory([FromBody] HistoryData historyData)
        {
            var history = await _messageService.GetMessageHistoryAsync(historyData.Sender, historyData.Receiver, historyData.isGroup);

            if (history == null || history.Count == 0)
            {
                return Ok(new { Message = "No History Found..."});
            }

            return Ok(history);
        }
    }
    public class HistoryData
    {
        public string Sender { get; set;}
        public string Receiver { get; set;}
        public bool isGroup { get; set;}
    }
}