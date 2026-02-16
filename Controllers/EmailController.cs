using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using Project2EmailNight.Dtos;

namespace Project2EmailNight.Controllers
{
    public class EmailController : Controller
    {
        public IActionResult SendEmail()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SendEmail(MailRequestDto mailRequestDto)
        {
            MimeMessage mimeMessage = new MimeMessage();

            var mailboxAddressFrom =
                new MailboxAddress("Şevval", "sevvalgulsahin44@gmail.com");
            mimeMessage.From.Add(mailboxAddressFrom);

            var mailboxAddressTo =
                new MailboxAddress("User", mailRequestDto.ReceiverEmail);
            mimeMessage.To.Add(mailboxAddressTo);

            mimeMessage.Subject = mailRequestDto.Subject;

            var bodyBuilder = new BodyBuilder
            {
                TextBody = mailRequestDto.MessageDetail
            };
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            using var smtpClient = new SmtpClient();

            // CRL / OCSP (sertifika iptal kontrolü) kapatıldı
            smtpClient.CheckCertificateRevocation = false;

            //  Gmail için doğru TLS
            smtpClient.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

            smtpClient.Authenticate(
                "sevvalgulsahin44@gmail.com",
                "ptpl oyqp ppfg jeos"
            );

            smtpClient.Send(mimeMessage);
            smtpClient.Disconnect(true);

            return RedirectToAction("SendEmail");
        }
    }
}
