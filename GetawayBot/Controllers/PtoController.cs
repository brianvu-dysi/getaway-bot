using GetawayBot.Data;
using GetawayBot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
		private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;
		private PtoRequest _currentRequest;
		private readonly string _appId;
		private readonly string _bearerToken;
		private readonly string _baseApiUrl;

		public PtoController(ICardService service, IBotFrameworkHttpAdapter adapter, IConfiguration configuration,
		ConcurrentDictionary<string, ConversationReference> conversationReferences)
		{
			_service = service;
			_adapter = adapter;
			_conversationReferences = conversationReferences;
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
			try
			{
				_currentRequest = pto;
				// make api calls to get email address
				var restClient = new RestClient(_baseApiUrl);

				var userRequest = new RestRequest($"user/{pto.EmployeeUserId}");
				userRequest.AddHeader("Authorization", $"Bearer {_bearerToken}");
				var userResponse = await restClient.ExecuteAsync(userRequest, Method.GET);
				var userEmail = JObject.Parse(userResponse.Content)["email"].ToString();
				pto.EmployeeEmail = userEmail;

				var managerRequest = new RestRequest($"user/{pto.ManagerUserId}");
				managerRequest.AddHeader("Authorization", $"Bearer {_bearerToken}");
				var managerResponse = await restClient.ExecuteAsync(managerRequest, Method.GET);
				var managerEmail = JObject.Parse(managerResponse.Content)["email"].ToString();
				pto.ManagerEmail = managerEmail;

				// find approprivate personal conversation
				var personalManagerConversation = _conversationReferences.Values.FirstOrDefault(
					x => x.User.Properties["upn"].ToString().ToLower() == pto.ManagerEmail.ToLower()
					&& x.Conversation.ConversationType == "personal");

				// don't do anything if no manager found
				if (personalManagerConversation == null) return new NotFoundResult();

				await ((BotAdapter)_adapter).ContinueConversationAsync(_appId, personalManagerConversation, CreatePtoCard, default);

				// posts the complete results back to the page
				return new OkObjectResult(pto);
			}
			catch (Exception ex)
			{
				throw ex;
			}
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