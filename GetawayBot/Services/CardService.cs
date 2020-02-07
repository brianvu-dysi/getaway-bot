using AdaptiveCards.Templating;
using GetawayBot.Data;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GetawayBot.Services
{
	public class CardService : ICardService
	{
		// returns card template if empty
		public Attachment CreatePtoCard(PtoRequest ptoData)
		{
			var ptoDataString = JsonConvert.SerializeObject(ptoData);
			// combine path for cross platform
			var populatedCard = !string.IsNullOrEmpty(ptoDataString) ? PopulateCard(ptoDataString) : LoadCardTemplate("approvalCard");

			var adaptiveCardAttachment = new Attachment()
			{
				ContentType = "application/vnd.microsoft.card.adaptive",
				Content = JsonConvert.DeserializeObject(populatedCard),
			};
			return adaptiveCardAttachment;
		}

		private string PopulateCard(string cardJson)
		{
			var templateJson = LoadCardTemplate("approvalCard");
			var transformer = new AdaptiveTransformer();
			var populatedCard = transformer.Transform(templateJson, cardJson);
			return populatedCard;
		}

		private string LoadCardTemplate(string templateName)
		{
			string[] paths = { ".", "Templates", $"{templateName}.json" };
			var cardTemplateJson = File.ReadAllText(Path.Combine(paths));
			return cardTemplateJson;
		}
	}
}
