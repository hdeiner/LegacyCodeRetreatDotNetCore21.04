using System;
using Xunit;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace TimeTeller
{
    public class TimeTellerTests
    {

        [Fact]
        public void localTimeCurrent()
        {
            Assert.Equal(getFormattedTime(DateTime.Now), TimeTeller.GetFormattedTime(TimeZones.LOCAL, TimeFormatting.NUMERIC, false));
        }

        [Fact]
        public void zuluTimeCurrent()
        {
            Assert.Equal(getFormattedTime(DateTime.UtcNow) + "Z", TimeTeller.GetFormattedTime(TimeZones.UTC, TimeFormatting.NUMERIC, false));
        }

        [Fact]
        public void localTimeInWords()
        {
            Assert.Matches("^(\\s|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|twenty|about|almost|a|little|after|quarter|half|of|past|before|at|night|in|the|morning|afternoon|evening|night)+$", TimeTeller.GetFormattedTime(TimeZones.LOCAL, TimeFormatting.APPROXIMATE_WORDING, false));
        }

        [Fact]
        public void zuluTimeInWords()
        {
            Assert.Matches("^(\\s|one|two|three|four|five|six|seven|eight|nine|ten|eleven|twelve|twenty|about|almost|a|little|after|quarter|half|of|past|before|at|night|in|the|morning|afternoon|evening|night)+Zulu$", TimeTeller.GetFormattedTime(TimeZones.UTC, TimeFormatting.APPROXIMATE_WORDING, false));
        }

        [Fact]
        public async System.Threading.Tasks.Task eMailForLocalTimeAsync()
        {
            String localTimeNowFormatted = getFormattedTime(DateTime.Now);
            TimeTeller.GetFormattedTime(TimeZones.LOCAL, TimeFormatting.NUMERIC, true);
            Boolean foundEmailSent = false;

            // Shamelessly Using code samples from https://developers.google.com/gmail/api/quickstart/dotnet 
            // and https://stackoverflow.com/questions/36448193/how-to-retrieve-my-gmail-messages-using-gmail-api

            // If modifying these scopes, delete your previously saved credentials
            // at ~/.credentials/gmail-dotnet-quickstart.json
            string[] Scopes = { GmailService.Scope.GmailReadonly };
            string ApplicationName = "Gmail API .NET Quickstart";

            UserCredential credential;

            using (var stream =
                new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
            }

            // Create Gmail API service.
            var gmailService = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            var emailListRequest = gmailService.Users.Messages.List("howarddeiner.xyzzy@gmail.com");
            emailListRequest.LabelIds = "INBOX";
            emailListRequest.IncludeSpamTrash = false;
            //emailListRequest.Q = "is:unread"; //this was added because I only wanted undread email's...

            //get our emails
            var emailListResponse = await emailListRequest.ExecuteAsync();

            if (emailListResponse != null && emailListResponse.Messages != null)
            {
                //loop through each email and get what fields you want...
                foreach (var email in emailListResponse.Messages)
                {

                    var emailInfoRequest = gmailService.Users.Messages.Get("howarddeiner.xyzzy@gmail.com", email.Id);
                    //make another request for that email id...
                    var emailInfoResponse = await emailInfoRequest.ExecuteAsync();

                    if (emailInfoResponse != null)
                    {
                        String from = "";
                        String date = "";
                        String subject = "";
                        String body = "";
                        //loop through the headers and get the fields we need...
                        foreach (var mParts in emailInfoResponse.Payload.Headers)
                        {
                            if (mParts.Name == "Date")
                            {
                                date = mParts.Value;
                            }
                            else if (mParts.Name == "From")
                            {
                                from = mParts.Value;
                            }
                            else if (mParts.Name == "Subject")
                            {
                                subject = mParts.Value;
                            }

                            if (date != "" && from != "")
                            {
                                if (emailInfoResponse.Payload.Parts == null && emailInfoResponse.Payload.Body != null)
                                {
                                    body = emailInfoResponse.Payload.Body.Data;
                                }
                                else
                                {
                                    body = getNestedParts(emailInfoResponse.Payload.Parts, "");
                                }
                                //need to replace some characters as the data for the email's body is base64
                                String codedBody = body.Replace("-", "+");
                                codedBody = codedBody.Replace("_", "/");
                                byte[] data = Convert.FromBase64String(codedBody);
                                body = Encoding.UTF8.GetString(data);

                                if (body.Equals("The time is now " + localTimeNowFormatted + "\r\n\r\n"))
                                {
                                        foundEmailSent = true;
                                }

                            }

                        }
                    }
                }
            }
            Assert.True(foundEmailSent);
        }

        static String getNestedParts(IList<MessagePart> part, string curr)
        {
            string str = curr;
            if (part == null)
            {
                return str;
            }
            else
            {
                foreach (var parts in part)
                {
                    if (parts.Parts == null)
                    {
                        if (parts.Body != null && parts.Body.Data != null)
                        {
                            str += parts.Body.Data;
                        }
                    }
                    else
                    {
                        return getNestedParts(parts.Parts, str);
                    }
                }

                return str;
            }

        }

        private String getFormattedTime(DateTime clock)
        {
            int localHour = clock.Hour;
            int localMinute = clock.Minute;
            int localSecond = clock.Second;
            return localHour.ToString("00") + ":" + localMinute.ToString("00") + ":" + localSecond.ToString("00");
        }
    }
}
