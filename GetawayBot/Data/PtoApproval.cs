using Newtonsoft.Json;

namespace GetawayBot.Data
{
	public enum ApprovalType
	{
		Approved, Rejected
	}

	public class PtoApproval
	{
		[JsonProperty("employeeUserId")] public string EmployeeUserId { get; set; }
		[JsonProperty("employeeName")] public string EmployeeName { get; set; }

		[JsonProperty("managerUserId")] public string ManagerUserId { get; set; }
		[JsonProperty("managerName")] public string ManagerName { get; set; }

		[JsonProperty("ptoType")] public string PtoType { get; set; }
		[JsonProperty("startDate")] public string StartDate { get; set; }
		[JsonProperty("endDate")] public string EndDate { get; set; }
		[JsonProperty("comments")] public string Comments { get; set; }
	}
}
