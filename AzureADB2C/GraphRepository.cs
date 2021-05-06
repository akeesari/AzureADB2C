using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace AzureADB2C
{
	public class GraphRepository : IDisposable
	{
		private readonly ILogger<GraphRepository> _logger;
		private readonly IMyConfiguration _config;

		public GraphRepository(IMyConfiguration configurationRoot, ILogger<GraphRepository> logger)
		{
			_logger = logger;
			_config = configurationRoot;
		}

		public static AuthenticationContext authContext;
		public static ClientCredential credential;
		public static HttpClient httpClient = new HttpClient();

		private static object lockObject = new object();

		public void Run()
		{
			var ldapUsers = new List<LdapUser>
			{
				new LdapUser { UserID = "Bkulp2012",PhoneNumber ="1234567890" }, 
				new LdapUser { UserID = "Bneeson",PhoneNumber = "1234567890" } 
			};

			ProcessB2CUsers(ldapUsers);
		}

		public void ProcessB2CUsers(List<LdapUser> ldapUsers)
		{
			var b2cphoneNoMissingUsers = new List<String>();
			var usersNotInB2C = new List<String>();
			authContext = new AuthenticationContext(_config.Instance + _config.Tenant);
			credential = new ClientCredential(_config.ClientID, _config.ClientSecret);
			HttpRequestMessage request;

			//Get accee token
			var acceeToken = authContext.AcquireTokenAsync(_config.Endpoint, credential).Result.AccessToken;
			//Console.WriteLine($"acceeToken - \n{acceeToken}\n");

			//Get B2C Users by UserId
			foreach (var item in ldapUsers.ToList())
			{
				var ldapUserId = item.UserID;
				B2CUser b2CUser = null;
				string filter = $"$select=displayName,id&$filter=identities/any(c:c/issuerAssignedId eq '{ldapUserId}' and c/issuer eq '{_config.Tenant}')";
				string getUserUrl = $"https://graph.microsoft.com/v1.0/users?{filter}";
				using (request = new HttpRequestMessage(HttpMethod.Get, getUserUrl))
				{
					request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", acceeToken);

					using (var response = httpClient.SendAsync(request).Result)
					{
						if (response.IsSuccessStatusCode)
						{
							string result = response.Content.ReadAsStringAsync().Result;
							b2CUser = JsonConvert.DeserializeObject<B2CUser>(result);
						}
					}
				}
				if (b2CUser.Value.Count == 0)
				{
					usersNotInB2C.Add(ldapUserId);
					continue;
				}

				//Get authentication Phone Number from Azure B2C by b2c user objectId
				string objectId = $"{b2CUser.Value[0].Id}";
				string getPhoneNoUrl = $"https://graph.microsoft.com/beta/users/{objectId}/authentication/phoneMethods/3179e48a-750b-4051-897c-87b9720928f7";
				using (request = new HttpRequestMessage(HttpMethod.Get, getPhoneNoUrl))
				{
					request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", acceeToken);

					using (var response = httpClient.SendAsync(request).Result)
					{
						if (response.IsSuccessStatusCode)
						{
							string result = response.Content.ReadAsStringAsync().Result;
							var phoneMethod = JsonConvert.DeserializeObject<PhoneMethods>(result);
							if (phoneMethod != null && !string.IsNullOrEmpty(phoneMethod.PhoneNumber))
							{
								//Console.WriteLine($"Phone Number Updated in B2C, usename-{ldapUserId}, PhoneNumber-{phoneMethod.PhoneNumber}");
							}
						}
						else
						{
							b2cphoneNoMissingUsers.Add(ldapUserId);
						}
					}
				}
			}

			_logger.LogInformation($"Phone Number missing users in B2C, Count-{b2cphoneNoMissingUsers.Count}");
			foreach (var item in b2cphoneNoMissingUsers)
			{
				_logger.LogInformation($"{item}");
			}

			_logger.LogInformation($"Users not in B2C, Count-{usersNotInB2C.Count}");
			foreach (var item in usersNotInB2C)
			{
				_logger.LogInformation($"{item}");
			}

		}

		void IDisposable.Dispose()
		{
			lock (lockObject)
			{
				//Cleanup objects here
				if (null == httpClient)
				{
					httpClient.Dispose();
					httpClient = null;
				}
				if (null == authContext)
				{
					authContext = null;
				}

				if (null == credential)
				{
					credential = null;
				}


			}

		}
	}

	public class B2CUser
	{
		public string OdataContext { get; set; }
		public List<B2CUserValue> Value { get; set; }
	}
	public class B2CUserValue
	{
		public string DisplayName { get; set; }
		public string Id { get; set; }
	}
	public class PhoneMethods
	{
		public string PhoneNumber { get; set; }
		public string PhoneType { get; set; }
	}
	public class LdapUser
	{
		public string UserID { get; set; }
		public string FName { get; set; }
		public string LName { get; set; }
		public string PreferredName { get; set; }
		public string PhoneNumber { get; set; }
		public string EmailAddress { get; set; }

	}
}
