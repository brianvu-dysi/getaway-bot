using GetawayBot.Data;
using GetawayBot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace GetawayBot.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PtoController : ControllerBase
    {
		private readonly ICardService _service;
		private readonly IBotFrameworkHttpAdapter _adapter;
		private readonly ILogger<PtoController> _logger;
		private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;
		private PtoRequest _currentRequest;
		private readonly string _appId;
		private readonly string _bearerToken;
		private readonly string _baseApiUrl;

		public PtoController(ICardService service, IBotFrameworkHttpAdapter adapter, IConfiguration configuration,
		ConcurrentDictionary<string, ConversationReference> conversationReferences, ILogger<PtoController> logger)
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
			_service = service;
			_adapter = adapter;
			_conversationReferences = conversationReferences;
			_logger = logger;
			_appId = configuration["MicrosoftAppId"];
			_bearerToken = configuration["DysiBearerToken"];
			_baseApiUrl = configuration["BaseApiUrl"];

			// If the channel is the Emulator, and authentication is not in use,
			// the AppId will be null.  We generate a random AppId for this case only.
			// This is not required for production, since the AppId will have a value.
			if (string.IsNullOrEmpty(_appId))
			{
				_appId = Guid.NewGuid().ToString(); //if no AppId, use a random Guid
			}
		}

		[HttpGet]
        public IActionResult Get()
        {
			_logger.LogInformation("Health check request received.");
			return new OkObjectResult(new { status = "OK" });
        }

		/// <summary>
		/// Creates an approval card
		/// </summary>
		/// <param name="pto"></param>
		/// <returns></return
		[HttpPost]
		public async Task<IActionResult> PostAsync([FromBody] PtoRequest pto)
		{
			_logger.LogInformation("Pto request received.", pto);
			_currentRequest = pto;

			// make api calls to get email address
			try
			{
				var restClient = new RestClient(_baseApiUrl);
				var managerRequest = new RestRequest($"user/{pto.ManagerUserId}", DataFormat.Json);
				managerRequest.AddHeader("Authorization", $"Bearer {_bearerToken}");
				var managerResponse = await restClient.ExecuteAsync(managerRequest, Method.GET);
				var managerEmail = JObject.Parse(managerResponse.Content)["email"].ToString();
				pto.ManagerEmail = managerEmail;
			}
			catch(Exception ex)
			{
				_logger.LogError("Unable to find manager");
				return new BadRequestObjectResult(pto);
			}

			// find approprivate personal conversation
			var personalManagerConversation = _conversationReferences.Values.FirstOrDefault(
				x => x.User.Properties["upn"].ToString().ToLower() == pto.ManagerEmail.ToLower()
				&& x.Conversation.ConversationType == "personal");

			_logger.LogInformation("Manager found.", personalManagerConversation.User.Name);

			// don't do anything if no manager found
			if (personalManagerConversation == null) return new NotFoundResult();

			await ((BotAdapter)_adapter).ContinueConversationAsync(_appId, personalManagerConversation, CreatePtoCard, default);

			_logger.LogInformation("Adaptive card sent.");

			// posts the complete results back to the page
			return new OkObjectResult(pto);
		}

		private async Task CreatePtoCard(ITurnContext turnContext, CancellationToken cancellationToken)
		{
			// Cards are sent as Attachments in the Bot Framework.
			// So we need to create a list of attachments for the reply activity.
			var attachments = new List<Attachment>();

			// Reply to the activity we received with an activity.
			var reply = MessageFactory.Attachment(attachments);
			reply.Attachments.Add(_service.CreatePtoCard(_currentRequest));
			await turnContext.SendActivityAsync(reply, cancellationToken);
		}
	}
}