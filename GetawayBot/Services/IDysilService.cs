using GetawayBot.Data;
using System.Threading.Tasks;

namespace GetawayBot.Services
{
	public interface IDysilService
	{
		Task<bool> SendApprovalAsync(PtoApproval pto, ApprovalType type);

	}
}
