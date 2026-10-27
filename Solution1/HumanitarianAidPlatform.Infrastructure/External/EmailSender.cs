using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks; 

namespace HumanitarianAidPlatform.Infrastructure.External
{
    public class EmailSender
    {
        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            
            await Task.CompletedTask;

            
            System.Diagnostics.Debug.WriteLine($"[Mock Email Sent] To: {to} | Subject: {subject} | Body: {body}");

            return true;
        }
    }
}