using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StopWatch.Jira
{
    class TempoClient
    {
        internal static string AccountId;
        internal static string Token;

        internal static bool PostWorklog(string key, DateTimeOffset startTime, TimeSpan timeElapsed, string comment, EstimateUpdateMethods estimateUpdateMethod, string estimateUpdateValue, string subprojectKey)
        {
            try
            {
                IRestRequest request = null;
                request = new RestRequest("/worklogs", Method.POST);
                request.AddHeader("Authorization", "Bearer " + Token);
                request.RequestFormat = DataFormat.Json;
                request.AddBody(new
                {
                    issueKey = key.Trim(),
                    timeSpentSeconds = timeElapsed.TotalSeconds,
                    billableSeconds = timeElapsed.TotalSeconds,
                    startDate = startTime.ToString("yyyy-MM-dd"),
                    description = comment,
                    authorAccountId = AccountId.Trim(),
                    attributes =
                        new[]
                        {
                            new
                            {
                                key = "_Commessa_",
                                value = subprojectKey
                            }
                        }
                }
                );

                RestClient client = new RestClient("https://api.tempo.io/4");
                var response = client.Execute(request);

                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }
        }
    }
}
