using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace TimeTeller
{
    public class TimeTeller
    {

        static void Main(string[] args)
        {

            Console.WriteLine(GetFormattedTime(TimeZones.LOCAL, TimeFormatting.NUMERIC, false));
            Console.WriteLine(GetFormattedTime(TimeZones.UTC, TimeFormatting.NUMERIC, false));
            Console.WriteLine(GetFormattedTime(TimeZones.LOCAL, TimeFormatting.APPROXIMATE_WORDING, false));
            Console.WriteLine(GetFormattedTime(TimeZones.UTC, TimeFormatting.APPROXIMATE_WORDING, false));
            Console.WriteLine(GetFormattedTime(TimeZones.LOCAL, TimeFormatting.NUMERIC, true));
            Console.WriteLine(GetFormattedTime(TimeZones.UTC, TimeFormatting.NUMERIC, true));
            Console.WriteLine(GetFormattedTime(TimeZones.LOCAL, TimeFormatting.APPROXIMATE_WORDING, true));
            Console.WriteLine(GetFormattedTime(TimeZones.UTC, TimeFormatting.APPROXIMATE_WORDING, true));
            Console.Read();
        }

        public static readonly int SECONDS_IN_A_HALF_MINUTE = 30;
        public static readonly int HOURS_IN_A_QUARTER_OF_A_DAY = 6;
        public static readonly int MINUTE_TO_START_FUZZING_INTO_NEXT_HOUR = 35;

        public static String GetFormattedTime(TimeZones whichTimeZone, TimeFormatting typeOfFormatting, Boolean eMailTimeFlag)
        {
            String formattedTime = FormatTime(whichTimeZone, typeOfFormatting);

            if (eMailTimeFlag)
            {
                SendEmail(formattedTime);
            }

            return formattedTime;
        }

        private static String FormatTime(TimeZones whichTimeZone, TimeFormatting typeOfFormatting)
        {
            String formattedTime = "";

            DateTimeOffset localDateTime = GetLocalDateTime(whichTimeZone);
            int hour = localDateTime.Hour;
            int minute = localDateTime.Minute;
            int second = localDateTime.Second;

            switch (typeOfFormatting)
            {
                case TimeFormatting.NUMERIC:
                    formattedTime = FormatTimeNumeric(hour, minute, second);
                    if (whichTimeZone == TimeZones.UTC)
                    {
                        formattedTime += "Z";
                    }
                    break;
                case TimeFormatting.APPROXIMATE_WORDING:
                    formattedTime = FormatTimeApproximateWording(hour, minute, second);
                    if (whichTimeZone == TimeZones.UTC)
                    {
                        formattedTime += " Zulu";
                    }
                    break;
            }

            return formattedTime;
        }

        private static DateTimeOffset GetLocalDateTime(TimeZones whichTimeZone)
        {
            DateTimeOffset localDateTime = DateTime.Now;
            switch (whichTimeZone)
            {
                case TimeZones.LOCAL:
                    localDateTime = new DateTimeOffset(DateTime.Now);
                    break;
                case TimeZones.UTC:
                    localDateTime = new DateTimeOffset(DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc));
                    break;
            }
            return localDateTime;

        }

        private static String FormatTimeNumeric(int hour, int minute, int second)
        {
            return hour.ToString("00") + ":" + minute.ToString("00") + ":" + second.ToString("00");
        }

        private static String FormatTimeApproximateWording(int hour, int minute, int second)
        {
            String formattedTime = "";

            String[] namesOfTheHours = { "twelve", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven" };
            String[] fuzzyTimeWords = { "about", "a little after", "about ten after", "about a quarter after", "about twenty after", "almost half past", "about half past", "almost twenty before", "about twenty before", "about a quarter of", "about ten of", "almost", "about" };
            String[] quadrantOfTheDay = { "at night", "in the morning", "in the afternoon", "in the evening" };

            if (second >= SECONDS_IN_A_HALF_MINUTE) minute++;

            formattedTime += fuzzyTimeWords[(minute + 2) / 5] + " ";

            if (minute < MINUTE_TO_START_FUZZING_INTO_NEXT_HOUR)
            {
                formattedTime += namesOfTheHours[hour % namesOfTheHours.Length];
            }
            else
            {
                formattedTime += namesOfTheHours[(hour + 1) % namesOfTheHours.Length];
            }

            formattedTime += " " + quadrantOfTheDay[hour / HOURS_IN_A_QUARTER_OF_A_DAY];

            return formattedTime;
        }

        private static void SendEmail(String formattedTime)
        { 
            try
            {
                NetworkCredential networkCredential = new NetworkCredential(
                    GetConfigProperty("smtpUserId"),
                    GetConfigProperty("smtpUserPassword")
                );

                MailMessage mailMessage = new MailMessage()
                {
                    From = new MailAddress(GetConfigProperty("emailFromAddress")),
                    Subject = GetConfigProperty("emailSubject"),
                    Body = GetConfigProperty("eMailMessage") + " " + formattedTime
                };

                mailMessage.To.Add(new MailAddress(GetConfigProperty("emailToAddress")));

                SmtpClient smtpClient = new SmtpClient()
                {
                    Port = Convert.ToInt32(GetConfigProperty("smtpPort")),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = Convert.ToBoolean(GetConfigProperty("smtpUseDefaultCredentials")),
                    Host = GetConfigProperty("smtpHost"),
                    EnableSsl = Convert.ToBoolean(GetConfigProperty("smtpEnableSsl")),
                    Credentials = networkCredential
                };

                smtpClient.Send(mailMessage);
            }
            catch (Exception ex)
            {
            }
        }

        private static String GetConfigProperty(String name)
        {
            String value = null;

            Regex matchPropertyName = new Regex(@"^(" + name + @"\=)(.*)$");

            String line = "";
            FileStream fileStream = new FileStream("config.properties", FileMode.Open);
            using (StreamReader reader = new StreamReader(fileStream))
            {
                do
                {
                    line = reader.ReadLine();
                    Match match = matchPropertyName.Match(line);
                    if (match.Success)
                    {
                        value = match.Groups[2].Value;
                    }
                } while ((line != null) && (value == null));
            } 
            return value;
        }
    }

}
