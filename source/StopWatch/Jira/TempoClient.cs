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

        internal static bool PostWorklog(int issueId, DateTimeOffset startTime, TimeSpan timeElapsed, string comment, EstimateUpdateMethods estimateUpdateMethod, string estimateUpdateValue, string subprojectKey)
        {
            try
            {
                IRestRequest request = null;
                request = new RestRequest("/worklogs", Method.POST);
                request.AddHeader("Authorization", "Bearer " + Token);
                request.RequestFormat = DataFormat.Json;
                request.AddBody(new
                {
                    issueId = issueId,
                    timeSpentSeconds = timeElapsed.TotalSeconds,
                    billableSeconds = RoundToNearestQuarterOfHour(timeElapsed.TotalSeconds),
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

        private static object RoundToNearestQuarterOfHour(double totalSeconds)
        {
            //round to multiples of 900 seconds, that is a quarter of hour
            return RoundToNearestMultipleOfFactor(totalSeconds, 900);
        }

        /// <summary>
        /// Rounds a number to the nearest multiple of another number.
        /// Source: https://stackoverflow.com/a/71606330/505893
        /// </summary>
        /// <param name="value">The value to round</param>
        /// <param name="factor">The factor to round to a multiple of. Must not be zero.</param>
        /// <param name="mode">Defines direction to round if <paramref name="value"/> is exactly halfway between two multiples of <paramref name="factor"/></param>
        /// <remarks>
        /// Use with caution when <paramref name="value"/> is large or <paramref name="factor"/> is small.
        /// </remarks>
        /// <exception cref="DivideByZeroException">If <paramref name="factor"/> is zero</exception>
        private static double RoundToNearestMultipleOfFactor(double value, double factor, MidpointRounding mode = MidpointRounding.AwayFromZero)
        {
            return Math.Round(value / factor, mode) * factor;
        }
    }
}
