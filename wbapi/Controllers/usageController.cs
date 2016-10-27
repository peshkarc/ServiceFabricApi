using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using vdbservices.Interfaces;
using Microsoft.ServiceFabric.Services.Client;
using System.Net;
using System.Net.Http;
using System.Threading;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus;
using C = System.Data.SqlClient; // System.Data.dll  
using System.Globalization;
using System.Configuration;
using vdb.msonline.helper;
using System.Web.Http;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace wbapi.Controllers
{
   // [System.Web.Http.RoutePrefix("api/V1/[controller]")]
    public class usageController : System.Web.Http.ApiController
    {
         string connstring = ConfigurationManager.AppSettings["SQLTableConnString"].ToString();               
         string sQueueName = ConfigurationManager.AppSettings["QueueName"].ToString();

        //----------------------------------------------------------------------------------------------------------
        //  Global variables: To hold teh maximum limit for granularity types
        //----------------------------------------------------------------------------------------------------------
        //private const int MaxYears  =   ;                     // No Limit
        private const int MaxMonths = 36;                       // 36 Months
        private const int MaxDays   = 365;                      // 365 Days
        private const int MaxWeeks  = 52;                       // 52 Weeks
        private const int MaxHours  = 31 * 24;                  // 31 Days * 24 hours
        private const int MaxQuater = 24 * (60 / 15);           // 24 hours * (60/15) min

        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/V1/usage/{connectionId}/{marketSegment}/{granularity}/{start}/{end}")] 
        [ValidateAntiForgeryToken]
        //----------------------------------------------------------------------------------------------------------
        //  Get the request from API and see if it meets all the criterias defined (Date range, data avilability, etc)
        //  then check if any data is avaiable for the given connection ID in Aggregation table
        //  If no data is available then submit a request for aggregation
        //----------------------------------------------------------------------------------------------------------
        public HttpResponseMessage Get(string connectionId,string marketSegment, string granularity,string start,string end)
        {
            string ClusterReference = "";
            DateTime sdate = new DateTime();
            DateTime edate = new DateTime();
            List<AggregationEntities> lstAgg = null;


            string sName = null;
            Electric.LDNLow ldnlow = null;
            Electric.LDNHigh ldnhigh = null;
            Electric.LDNSingle ldnsingle = null;
            Electric.ODNLow odnlow = null;
            Electric.ODNHigh odnhigh = null;
            Electric.ODNSingle odnsingle = null;

            Electric.Consumption objConsumption = null;
            Gas.Consumption objGasConsumption = null;
            Gas.LDNSingle gldnsingle = null;
            
            List<Electric.Consumption> lstconsumption = new List<Electric.Consumption>();
            List<Gas.Consumption> lstGasconsumption = new List<Gas.Consumption>();

            var counter = ServiceProxy.Create<ICounter>(new Uri("fabric:/Vanderbronsf/sfs"), new ServicePartitionKey(1));

            string sKey = connectionId + " - " + marketSegment + " - " + granularity + " - " + start + " - " + end;

            string sMarketSegment = marketSegment;//"Electricity";
            try
            {
                if (String.IsNullOrEmpty(connectionId) || String.IsNullOrEmpty(marketSegment) || String.IsNullOrEmpty(granularity) || String.IsNullOrEmpty(start) || String.IsNullOrEmpty(end))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "All Parameters are required.");
                }
                else
                {
                    if (!ValidateConnectionID(connectionId, ref ClusterReference))
                    {
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid ConnectionID.");
                    }
                    else if (marketSegment == "Gas" && granularity == "Q")
                    {
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid granularity for this MarketSegment.");
                    }
                    else if (marketSegment != "Gas" && marketSegment != "Electricity")
                    {
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid MarketSegment.");
                    }
                    else if (!Validate("Granularity", granularity))
                    {
                        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid granularity.");
                    }
                    else
                    {
                        long lResultCnt = ValidateParamters(granularity, start, end, ref sdate, ref edate);
                        string sRet = CheckDate(connectionId, sMarketSegment, sdate);
                        if (sRet != "Valid")
                        {
                            return Request.CreateErrorResponse(HttpStatusCode.BadRequest, sRet);
                        }
                        else
                        {
                            AggregationRequest AggReq = new AggregationRequest();
                            AggReq.ConnectionID = connectionId;
                            AggReq.Aggregationtype = granularity;
                            AggReq.marketsegment = sMarketSegment;
                            AggReq.Fromdate = sdate;
                            AggReq.Todate = edate;
                            AggReq.RequestSource = "API";

                            if (counter.GetDetails(AggReq).Result.ToList<AggregationEntities>().Count() == lResultCnt)
                            {
                                counter.Remove(sKey);

                                lstAgg = counter.GetDetails(AggReq).Result;
                                if (marketSegment == "Gas")
                                {
                                    Controllers.Gas.Request req = new Controllers.Gas.Request();
                                    req.connectionId = connectionId;
                                    req.ClusterReference = ClusterReference;
                                    req.MarketSegment = marketSegment;
                                    req.granularity = granularity;
                                    req.start = start;
                                    req.end = end;

                                    foreach (AggregationEntities agg in lstAgg)
                                    {
                                        sName = GetNameByGranularity(granularity, agg.aggfromdate);  // For Day
                                        objGasConsumption = new Gas.Consumption();
                                        objGasConsumption.name = sName;

                                        gldnsingle = new Gas.LDNSingle();
                                        gldnsingle.start = String.IsNullOrEmpty(Convert.ToString(agg.StartLDNSingle)) ? "0" : Convert.ToString(agg.StartLDNSingle);
                                        gldnsingle.end = String.IsNullOrEmpty(Convert.ToString(agg.ENDLDNSingle)) ? "0" : Convert.ToString(agg.ENDLDNSingle);
                                        gldnsingle.consumption = String.IsNullOrEmpty(Convert.ToString(agg.LDNGasPositionUsage)) ? "0" : Convert.ToString(agg.LDNGasPositionUsage);

                                        objGasConsumption.LDNSingle = gldnsingle;
                                        // objGasConsumption.GasUsage = String.IsNullOrEmpty(Convert.ToString(agg.LDNGasPositionUsage)) ? "0" : Convert.ToString(agg.LDNGasPositionUsage);
                                        lstGasconsumption.Add(objGasConsumption);
                                    }
                                    req.consumption = lstGasconsumption;
                                    return Request.CreateResponse<Controllers.Gas.Root>(HttpStatusCode.OK, new Gas.Root(req));
                                }
                                else
                                {
                                    Controllers.Electric.Request req = new Controllers.Electric.Request();
                                    req.connectionId = connectionId;
                                    req.ClusterReference = ClusterReference;
                                    req.MarketSegment = marketSegment;
                                    req.granularity = granularity;
                                    req.start = start;
                                    req.end = end;

                                    foreach (AggregationEntities agg in lstAgg)
                                    {
                                        sName = GetNameByGranularity(granularity, agg.aggfromdate);  // For Day

                                        ldnlow = new Electric.LDNLow();
                                        ldnlow.start = String.IsNullOrEmpty(Convert.ToString(agg.StartLDNLow)) ? "0" : Convert.ToString(agg.StartLDNLow);
                                        ldnlow.end = String.IsNullOrEmpty(Convert.ToString(agg.EndLDNLow)) ? "0" : Convert.ToString(agg.EndLDNLow);
                                        ldnlow.consumption = String.IsNullOrEmpty(Convert.ToString(agg.LDNLowUsage)) ? "0" : Convert.ToString(agg.LDNLowUsage);

                                        ldnhigh = new Electric.LDNHigh();
                                        ldnhigh.start = String.IsNullOrEmpty(Convert.ToString(agg.StartLDNHigh)) ? "0" : Convert.ToString(agg.StartLDNHigh);
                                        ldnhigh.end = String.IsNullOrEmpty(Convert.ToString(agg.EndLDNHigh)) ? "0" : Convert.ToString(agg.EndLDNHigh);
                                        ldnhigh.consumption = String.IsNullOrEmpty(Convert.ToString(agg.LDNHighUsage)) ? "0" : Convert.ToString(agg.LDNHighUsage);

                                        ldnsingle = new Electric.LDNSingle();
                                        ldnsingle.start = String.IsNullOrEmpty(Convert.ToString(agg.StartLDNSingle)) ? "0" : Convert.ToString(agg.StartLDNSingle);
                                        ldnsingle.end = String.IsNullOrEmpty(Convert.ToString(agg.ENDLDNSingle)) ? "0" : Convert.ToString(agg.ENDLDNSingle);
                                        ldnsingle.consumption = String.IsNullOrEmpty(Convert.ToString(agg.LDNUsage)) ? "0" : Convert.ToString(agg.LDNUsage);


                                        odnlow = new Electric.ODNLow();
                                        odnlow.start = String.IsNullOrEmpty(Convert.ToString(agg.StartODNLow)) ? "0" : Convert.ToString(agg.StartODNLow);
                                        odnlow.end = String.IsNullOrEmpty(Convert.ToString(agg.ENDODNLow)) ? "0" : Convert.ToString(agg.ENDODNLow);
                                        odnlow.consumption = "";

                                        odnhigh = new Electric.ODNHigh();
                                        odnhigh.start = String.IsNullOrEmpty(Convert.ToString(agg.StartODNHigh)) ? "0" : Convert.ToString(agg.StartODNHigh);
                                        odnhigh.end = String.IsNullOrEmpty(Convert.ToString(agg.EndODNHigh)) ? "0" : Convert.ToString(agg.EndODNHigh);
                                        odnhigh.consumption = "";

                                        odnsingle = new Electric.ODNSingle();
                                        odnsingle.start = "";
                                        odnsingle.end = "";
                                        odnsingle.consumption = "";

                                        objConsumption = new Electric.Consumption();
                                        objConsumption.name = sName;
                                        objConsumption.LDNLow = ldnlow;
                                        objConsumption.LDNHigh = ldnhigh;
                                        objConsumption.LDNSingle = ldnsingle;
                                        objConsumption.ODNLow = odnlow;
                                        objConsumption.ODNHigh = odnhigh;
                                        objConsumption.ODNSingle = odnsingle;

                                        lstconsumption.Add(objConsumption);
                                    }
                                    req.consumption = lstconsumption;

                                    return Request.CreateResponse<Controllers.Electric.Root>(HttpStatusCode.OK, new Electric.Root(req));
                                }
                            }
                            else
                            {
                                AggregationRequest objReq = counter.Get(sKey).Result;
                                if (objReq == null)
                                {
                                    InsertInQueue(AggReq);
                                    counter.Save(AggReq, sKey);
                                    return Request.CreateErrorResponse(HttpStatusCode.Accepted, "Request accepted, and queued for execution");
                                }
                                else
                                {
                                    return Request.CreateErrorResponse(HttpStatusCode.Accepted, "Request is in Progress, Please try after some time.");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog("API", connectionId, start, end, "Aggregation Request", ex.Message, "");
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest,ex.Message);
            }
        }
        //----------------------------------------------------------------------------------------------------------
        //  Check if the user has any data available in azure storange table in the given date range
        //----------------------------------------------------------------------------------------------------------
        public string CheckDate(string sConn, string sMkt, DateTime sStart)
        {
            var counter = ServiceProxy.Create<ICounter>(new Uri("fabric:/Vanderbronsf/sfs"), new ServicePartitionKey(1));
            string sDate = counter.GetLastElement(sConn, sStart.ToString(), sMkt).Result;
            if (sDate != "Invalid")
            {
                if (Convert.ToDateTime(sDate) > sStart)
                {
                    return "Please select a date range beyond: " + sDate;
                }
                else
                {
                    return "Valid";
                }
            }
            else
            {
                return "Customer Does not Exist.";
            }
        }
        //For Submitting Request in Aggregation Queue if Aggregation data not exists
        //----------------------------------------------------------------------------------------------------------
        //  Submit the request for Aggregation
        //----------------------------------------------------------------------------------------------------------
        protected void SubmitRequestInQuque(AggregationRequest Aggreq, long lResultCnt)
        {
            AggregationRequest objAggReq = null;
            DateTime sDate = new DateTime();
            DateTime eDate = new DateTime();

            for (int i = 0; i < lResultCnt; i++)
            {
                sDate = (i == 0) ? Aggreq.Fromdate : eDate;

                switch (Aggreq.Aggregationtype)
                {
                    case "Y":
                        eDate = sDate.AddYears(1);
                        break;
                    case "M":
                        eDate = sDate.AddMonths(1);
                        break;
                    case "D":
                        eDate = sDate.AddDays(1);
                        break;
                    case "W":
                        eDate = sDate.AddDays(7);
                        break;
                    case "H":
                        eDate = sDate.AddHours(1);
                        break;
                    case "Q":
                        eDate = sDate.AddMinutes(15);
                        break;
                }

                objAggReq = new AggregationRequest();
                objAggReq.ConnectionID = Aggreq.ConnectionID;
                objAggReq.Aggregationtype = Aggreq.Aggregationtype;
                objAggReq.marketsegment = Aggreq.marketsegment;
                objAggReq.Fromdate = sDate;
                objAggReq.Todate = (Aggreq.marketsegment == "Gas")? eDate : eDate.AddMinutes(-15);


                InsertInQueue(objAggReq);

            }
        }
        //----------------------------------------------------------------------------------------------------------
        //  Get the end date for the request based on the date range criteria
        //----------------------------------------------------------------------------------------------------------
        protected DateTime GetEndDate(string marketsegment,string granularity,DateTime EndDate)
        {
            DateTime result = EndDate;
            switch (granularity)
            {
                case "Y":
                    result = EndDate.AddYears(1);
                    break;
                case "M":
                    result = EndDate.AddMonths(1);
                    break;
                case "D":
                    result = EndDate.AddDays(1);
                    break;
                case "W":
                    result = EndDate.AddDays(7);
                    break;
                case "H":
                    result = EndDate.AddHours(1);
                    break;
                case "Q":
                    result = EndDate.AddMinutes(15);
                    break;
            }
            return (marketsegment == "Gas") ? result : result.AddMinutes(-15);
        }
        //----------------------------------------------------------------------------------------------------------
        //  Insert the requet in Aggregation queue
        //----------------------------------------------------------------------------------------------------------
        protected void InsertInQueue(AggregationRequest Req)
        {
            string sConnectionString = "Endpoint=sb://namevdbanalyticssb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=iPG1toRxfEuo8JD2Hye7DzX/nnfJUtwvBn8Q5znceKY=";
            // Create the queue if it does not exist already             
            var namespaceManager = NamespaceManager.CreateFromConnectionString(sConnectionString);
            if (!namespaceManager.QueueExists(sQueueName))
            {
                namespaceManager.CreateQueue(sQueueName);
            }
            // Initialize the connection to Service Bus Queue
            var client = QueueClient.CreateFromConnectionString(sConnectionString, sQueueName);

            // Create message, with the message body being automatically serialized
            var brokeredMessage = new BrokeredMessage(Req);
            // Send Request
            client.Send(brokeredMessage);
        }
        //----------------------------------------------------------------------------------------------------------
        //  This function is used for validating the granularity based on user inputs
        //----------------------------------------------------------------------------------------------------------
        protected bool Validate(string Type,string value)
        {
            bool result = false;

            if (Type == "ConnectionID")
            {
                result = true;
            }
            else if (Type == "Granularity")
            {
                switch (value)
                {
                    case "Y": result = true; break;
                    case "M": result = true; break;
                    case "D": result = true; break;
                    case "W": result = true; break;
                    case "H": result = true; break;
                    case "Q": result = true; break;
                    default: result = false; break;
                }
            }          
            
            
            return result;
        }

        //----------------------------------------------------------------------------------------------------------
        // For Validating ConnectionID & Getting ClusterReference
        //----------------------------------------------------------------------------------------------------------
        public bool ValidateConnectionID(string connectionID, ref string ClusterReference)
        {
            bool result = false;

            using (var connection = new C.SqlConnection(connstring))
            {
                connection.Open();
                C.SqlCommand command = new C.SqlCommand();
                command.Connection = connection;
                command.CommandTimeout = 0;
                command.CommandText = @"SELECT ClusterReference from dbo.connection where connectionID = '" + connectionID + "'";
                C.SqlDataReader dr = command.ExecuteReader();
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        ClusterReference = Convert.ToString(dr[0]);
                        result = true;
                    }
                }
                else
                {
                    result = false;
                }
                connection.Close();
            }
            return result;
        }
        //----------------------------------------------------------------------------------------------------------
        // For Validating Start & End Parameter and return Start Date & End Date as Per sGranularity
        //----------------------------------------------------------------------------------------------------------
        protected long ValidateParamters(string sGranularity, string start, string end, ref DateTime oStartDate, ref DateTime oEndDate)
        {
            long lResult = 0;

            oStartDate = DateTime.Now;
            oEndDate = DateTime.Now;

            sGranularity = sGranularity.Trim().ToUpper();

            switch (sGranularity)
            {
                // Year
                case "Y":
                    //NNNN
                    string sYear = start;
                    string eYear = end;

                    DateTime sdtYear = DateTime.ParseExact(sYear, "yyyy", null);
                    DateTime edtYear = DateTime.ParseExact(eYear, "yyyy", null);

                    oStartDate = sdtYear;
                    oEndDate = edtYear.AddYears(1).AddDays(-1).AddHours(23).AddMinutes(45);

                    lResult = DateDiff(DateInterval.Year, oStartDate, oEndDate.AddMinutes(15));
                    
                    break;

                // Month
                case "M":
                    //NNNN-MM
                    string sMth = start;
                    string eMth = end;

                    DateTime sdtMth = DateTime.ParseExact(sMth, "yyyy-MM", null);
                    DateTime edtMth = DateTime.ParseExact(eMth, "yyyy-MM", null);

                    oStartDate = sdtMth;
                    oEndDate = edtMth.AddMonths(1).AddDays(-1).AddHours(23).AddMinutes(45);
                    lResult = DateDiff(DateInterval.Month, oStartDate, oEndDate.AddMinutes(15));

                    //Check Constraint
                    if (lResult > MaxMonths) throw new Exception("Invalid Range for this granularity Type");

                    break;

                // Day
                case "D":

                    //NNNN-MM-dd
                    string sDay = start;
                    string eDay = end;

                    DateTime sdate = DateTime.ParseExact(sDay, "yyyy-MM-dd", null);
                    DateTime edate = DateTime.ParseExact(eDay, "yyyy-MM-dd", null);

                    oStartDate = sdate;
                    oEndDate = edate.AddHours(23).AddMinutes(45);

                    lResult = DateDiff(DateInterval.Day, oStartDate, oEndDate.AddMinutes(15));
                    //Check Constraint
                    if (lResult > MaxDays) throw new Exception("Invalid Range for this granularity Type");
                    break;

                // Week
                case "W":
                    // NNNN-WW
                    string sYearWk = start;
                    int isWkYear = Convert.ToInt32((sYearWk).Split('-')[0]);
                    int isWkNo = Convert.ToInt32((sYearWk).Split('-')[1]);

                    string eYearWk = end;
                    int ieWkYear = Convert.ToInt32((eYearWk).Split('-')[0]);
                    int ieWkNo = Convert.ToInt32((eYearWk).Split('-')[1]);

                    // First Date of Week
                    DateTime sdtWF = FirstDateOfWeek(isWkYear, isWkNo);
                    // Last Date of Week
                    DateTime edtWL = LastDateOfWeek(ieWkYear, ieWkNo);

                    oStartDate = sdtWF;
                    oEndDate = edtWL.AddHours(23).AddMinutes(45);

                    lResult = DateDiff(DateInterval.WeekOfYear, oStartDate, oEndDate.AddMinutes(15));

                    //Check Constraint
                    if (lResult > MaxWeeks) throw new Exception("Invalid Range for this granularity Type");

                    break;

                // Hours
                case "H":
                    //NNNN-MM-dd-HH
                    string sHour = start;
                    string eHour = end;

                    DateTime sdtHour = DateTime.ParseExact(sHour, "yyyy-MM-dd-HH", null);
                    DateTime edtHour = DateTime.ParseExact(eHour, "yyyy-MM-dd-HH", null);

                    oStartDate = sdtHour;
                    oEndDate = edtHour.AddMinutes(45);

                    lResult = DateDiff(DateInterval.Hour, oStartDate, oEndDate.AddMinutes(15));

                    //Check Constraint
                    if (lResult > MaxHours) throw new Exception("Invalid Range for this granularity Type");

                    break;

                // Quater (15 Mins)
                case "Q":

                    //NNNN-MM-dd-Q
                    string sHQuater = start;
                    int isYears = Convert.ToInt32((sHQuater).Split('-')[0]);
                    int isMonth = Convert.ToInt32((sHQuater).Split('-')[1]);
                    int isDay = Convert.ToInt32((sHQuater).Split('-')[2]);
                    int isHQ = Convert.ToInt32((sHQuater).Split('-')[3]);

                    DateTime sdtHqtr = new DateTime(isYears, isMonth, isDay);

                    sdtHqtr = sdtHqtr.AddMinutes(isHQ * 15);
                   
                    string eHQuater = end;
                    int ieYears = Convert.ToInt32((eHQuater).Split('-')[0]);
                    int ieMonth = Convert.ToInt32((eHQuater).Split('-')[1]);
                    int ieDay = Convert.ToInt32((eHQuater).Split('-')[2]);
                    int ieHQ = Convert.ToInt32((eHQuater).Split('-')[3]);

                    DateTime edtHqtr = new DateTime(ieYears, ieMonth, ieDay);

                    edtHqtr = edtHqtr.AddMinutes(ieHQ * 15);
                   
                    oStartDate = sdtHqtr;
                    oEndDate = edtHqtr;

                    lResult = DateDiff(DateInterval.Minute, sdtHqtr, edtHqtr) / 15;

                    //Check Constraint
                    if (lResult > MaxQuater) throw new Exception("Invalid Range for this granularity Type");

                    break;

                // Unknown
                default:
                    throw new Exception("Invalid Granularity");
            }

            //Date Validation
            if (oStartDate > oEndDate)
                throw new Exception("End parameter should be greater than or equal to start Paramter");
           // lResult = (lResult == 0) ? 1 : lResult;
            return lResult;
        }
        //----------------------------------------------------------------------------------------------------------
        //Get First Day by Week No.
        //----------------------------------------------------------------------------------------------------------
        protected DateTime FirstDateOfWeek(int year, int weekOfYear)
        {
            var firstDate = new DateTime(year, 1, 4);
            //first thursday of the week defines the first week (https://en.wikipedia.org/wiki/ISO_8601)
            //Wiki: the 4th of january is always in the first week
            while (firstDate.DayOfWeek != DayOfWeek.Monday)
                firstDate = firstDate.AddDays(-1);

            return firstDate.AddDays((weekOfYear - 1) * 7);
        }
        //----------------------------------------------------------------------------------------------------------
        // Get Last Day by Week No.
        //----------------------------------------------------------------------------------------------------------
        protected DateTime LastDateOfWeek(int year, int weekOfYear)
        {
            var firstDate = new DateTime(year, 1, 4);
            //first thursday of the week defines the first week (https://en.wikipedia.org/wiki/ISO_8601)
            //Wiki: the 4th of january is always in the first week
            while (firstDate.DayOfWeek != DayOfWeek.Sunday)
                firstDate = firstDate.AddDays(1);

            return firstDate.AddDays((weekOfYear - 1) * 7);
        }
        //----------------------------------------------------------------------------------------------------------
        // For Getting Date Difference
        //----------------------------------------------------------------------------------------------------------
        protected long DateDiff(DateInterval intervalType, DateTime fromDate, DateTime toDate)
        {
            switch (intervalType)
            {
                case DateInterval.Day:
                case DateInterval.DayOfYear:
                    System.TimeSpan spanForDays = toDate - fromDate;
                    return (long)spanForDays.TotalDays;
                case DateInterval.Hour:
                    System.TimeSpan spanForHours = toDate - fromDate;
                    return (long)spanForHours.TotalHours;
                case DateInterval.Minute:
                    System.TimeSpan spanForMinutes = toDate - fromDate;
                    return (long)spanForMinutes.TotalMinutes;
                case DateInterval.Month:
                    return ((toDate.Year - fromDate.Year) * 12) + (toDate.Month - fromDate.Month);
                case DateInterval.Quarter:
                    long fromDateQuarter = (long)System.Math.Ceiling(fromDate.Month / 3.0);
                    long toDateQuarter = (long)System.Math.Ceiling(toDate.Month / 3.0);
                    return (4 * (toDate.Year - fromDate.Year)) + toDateQuarter - fromDateQuarter;
                case DateInterval.Second:
                    System.TimeSpan spanForSeconds = toDate - fromDate;
                    return (long)spanForSeconds.TotalSeconds;
                case DateInterval.Weekday:
                    System.TimeSpan spanForWeekdays = toDate - fromDate;
                    return (long)(spanForWeekdays.TotalDays / 7.0);
                case DateInterval.WeekOfYear:
                    System.DateTime fromDateModified = fromDate;
                    System.DateTime toDateModified = toDate;
                    while (toDateModified.DayOfWeek != System.Globalization.DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek)
                    {
                        toDateModified = toDateModified.AddDays(-1);
                    }
                    while (fromDateModified.DayOfWeek != System.Globalization.DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek)
                    {
                        fromDateModified = fromDateModified.AddDays(-1);
                    }
                    System.TimeSpan spanForWeekOfYear = toDateModified - fromDateModified;
                    return (long)(spanForWeekOfYear.TotalDays / 7.0);
                case DateInterval.Year:
                    return toDate.Year - fromDate.Year;
                default:
                    return 0;
            }
        }
        //----------------------------------------------------------------------------------------------------------
        // Structire to hold the date details
        //----------------------------------------------------------------------------------------------------------
        public enum DateInterval
        {
            Day,
            DayOfYear,
            Hour,
            Minute,
            Month,
            Quarter,
            Second,
            Weekday,
            WeekOfYear,
            Year
        }
        //----------------------------------------------------------------------------------------------------------
        // For Getting Name property of Response message by Date & Granularity
        //----------------------------------------------------------------------------------------------------------
        protected string GetNameByGranularity(string Granularity,DateTime sdate)
        {
            string result = "";
            switch(Granularity)
            {
                case "Y": result = sdate.ToString("yyyy"); break;
                case "M": result = sdate.ToString("yyyy-MM"); break;
                case "D": result = sdate.ToString("yyyy-MM-dd"); break;
                case "W": result = sdate.ToString("yyyy") +"-"+ new GregorianCalendar(GregorianCalendarTypes.Localized).GetWeekOfYear(sdate, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);  break;
                case "H": result = sdate.ToString("yyyy-MM-dd-HH"); break;
                case "Q": result = sdate.ToString("yyyy-MM-dd") + (sdate.Minute/15); break;
            }
            return result;
        }
        //----------------------------------------------------------------------------------------------------------
        // Exception Logging Method
        //----------------------------------------------------------------------------------------------------------
        public void AddLog(string CallingProcess, string ConnId, string From, string To, string CallingFunction, string ExceptionMessage, string Comments)
        {                       
            using (var connection = new C.SqlConnection(connstring))
            {
                connection.Open();
                C.SqlCommand command = new C.SqlCommand();
                command.Connection = connection;
                command.CommandTimeout = 0;
                command.CommandText = @"INSERT INTO dbo.ErrorLogs (CallingProcess, ConnId, DateFrom, "
                                        + "DateTo, CallingFunction, ExceptionMessage, Comments) VALUES("
                                        + "'" + CallingProcess + "','" + ConnId + "','"
                                        + From + "','" + To + "','" + CallingFunction + "','"
                                        + ExceptionMessage + "','" + Comments + "')";
                command.ExecuteNonQuery();
                connection.Close();
            }
        }
    }
}
