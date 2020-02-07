using GetawayBot.Data;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Threading.Tasks;

namespace GetawayBot.Services
{
	public class DysilService : IDysilService
	{
		private readonly string _bearerToken;
		private readonly string _approvalScriptId;
		private readonly string _rejectionScriptId;
		private readonly string _dedicatedAreaScriptId;
		private readonly IRestClient _restClient;

		public DysilService(IConfiguration configuration)
		{
			_restClient = new RestClient(configuration["BaseApiUrl"]);
			_bearerToken = configuration["DysiBearerToken"];
			_approvalScriptId = configuration["ApprovalScriptId"];
			_rejectionScriptId = configuration["RejectiongScriptId"];
			_dedicatedAreaScriptId = configuration["DedicatedAreaScriptId"];
		}

		public async Task<bool> SendApprovalAsync(PtoApproval pto, ApprovalType type)
		{
			var executionToken = await GetExecutionTokenAsync();
			var scriptId = type == ApprovalType.Approved ? _approvalScriptId : _rejectionScriptId;
			var approveRequest = new RestRequest($"scripts/{scriptId}/run");
			approveRequest.AddHeader("Authorization", $"Bearer {_bearerToken}");
			approveRequest.AddHeader("Content-Type", "application/json");
			approveRequest.AddJsonBody(new
			{
				id = _approvalScriptId,
				scriptExecutionToken = executionToken,
				data = new
				{
					employeeUserId = pto.EmployeeUserId,
					employeeName = pto.EmployeeName,
					managerUserId = pto.ManagerUserId,
					managerName = pto.ManagerName,
					startDate = pto.StartDate,
					endDate = pto.EndDate,
					comments = pto.Comments,
					ptoType = pto.PtoType
				}
			});
			var approveResponse = await _restClient.ExecuteAsync(approveRequest, Method.POST);
			return approveResponse.StatusCode == System.Net.HttpStatusCode.OK;
		}

		private async Task<string> GetExecutionTokenAsync()
		{
			var tokenRequest = new RestRequest($"pages/{_dedicatedAreaScriptId}/sections");
			var tokenResponse = await _restClient.ExecuteAsync(tokenRequest, Method.GET);
			var token = JObject.Parse(tokenResponse.Content)["scriptExecutionToken"].ToString();
			return token;
		}
	}
}
