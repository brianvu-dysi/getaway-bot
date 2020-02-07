// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.6.2

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GetawayBot.Data;
using GetawayBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace GetawayBot.Bots
{
	public class TeamsBot : ActivityHandler
	{
		private readonly IDysilService _dysilService;
		private ConcurrentDictionary<string, ConversationReference> _conversationReferences;

		public TeamsBot(ConcurrentDictionary<string, ConversationReference> conversationReferences, IDysilService dysilService)
		{
			_dysilService = dysilService;
			_conversationReferences = conversationReferences;
		}

		protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
		{
			var text = turnContext.Activity.Text;

			// add conversation reference
			var members = await TeamsInfo.GetMembersAsync(turnContext, cancellationToken);
			var upn = members.FirstOrDefault(x => x.Id == turnContext.Activity.From.Id).UserPrincipalName;
			AddConversationReference(turnContext.Activity as Activity, upn);

			// handle regular messages with an echo
			if (turnContext?.Activity?.Value == null)
			{
				await turnContext.SendActivityAsync(MessageFactory.Text($"You sent '{turnContext.Activity.Text}'"), cancellationToken);
				return;
			}

			// handle approval or rejection card actions
			var value = JObject.Parse(turnContext.Activity?.Value?.ToString());
			var pto = value.ToObject<PtoApproval>();
			if (value?["action"].ToString() == "approve")
			{
				var approvalSuccessful = await _dysilService.SendApprovalAsync(pto, ApprovalType.Approved);
				if (!approvalSuccessful)
				{
					await turnContext.SendActivityAsync(MessageFactory.Text($"Error occurred while approving the request"), cancellationToken);
					return;
				}
				await turnContext.SendActivityAsync(MessageFactory.Text($"You have approved the request"), cancellationToken);
				// TODO: update card
				return;
			}
			else if (value?["action"].ToString() == "reject")
			{
				var rejectionSuccessful = await _dysilService.SendApprovalAsync(pto, ApprovalType.Rejected);
				if (!rejectionSuccessful)
				{
					await turnContext.SendActivityAsync(MessageFactory.Text($"Error occurred while rejecting the request"), cancellationToken);
					return;
				}
				await turnContext.SendActivityAsync(MessageFactory.Text($"You have rejected the request"), cancellationToken);
				// TODO: update card
				return;
			}
		}

		protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
		{
			var members = await TeamsInfo.GetMembersAsync(turnContext, cancellationToken);
			var upn = members.FirstOrDefault(x => x.Id == turnContext.Activity.From.Id).UserPrincipalName;

			var conversationReference = turnContext.Activity.GetConversationReference();
			conversationReference.User.Properties["upn"] = upn;

			_conversationReferences.AddOrUpdate(conversationReference.User.Id, conversationReference, (key, newValue) => conversationReference);
			var welcomeText = "Welcome to the PTO Bot!";
			foreach (var member in membersAdded)
			{
				if (member.Id != turnContext.Activity.Recipient.Id)
				{
					await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
				}
			}
		}

		protected override Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
		{
			AddConversationReference(turnContext.Activity as Activity);

			return base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);
		}

		private void AddConversationReference(Activity activity, string upn)
		{
			var conversationReference = activity.GetConversationReference();

			conversationReference.User.Properties["upn"] = upn;

			_conversationReferences.AddOrUpdate(conversationReference.User.Id, conversationReference, (key, newValue) => conversationReference);
		}

		private void AddConversationReference(Activity activity)
		{
			var conversationReference = activity.GetConversationReference();

			_conversationReferences.AddOrUpdate(conversationReference.User.Id, conversationReference, (key, newValue) => conversationReference);
		}
	}
}
