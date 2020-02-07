using GetawayBot.Data;
using Microsoft.Bot.Schema;

namespace GetawayBot.Services
{
	public interface ICardService
	{
		Attachment CreatePtoCard(PtoRequest ptoData);
	}
}
