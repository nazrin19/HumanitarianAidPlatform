using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace HumanitarianAidPlatform.Infrastructure.External
{
    public class SmsSender
    {
        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            
            await Task.CompletedTask;

            
            System.Diagnostics.Debug.WriteLine($"[Mock SMS Sent] To: {phoneNumber} | Message: {message}");

            return true;
        }
    }
}