module EmailService
open System.Net.Mail

let sendEmailInternal (smtpServer: string, port: int, ssl: bool, username: string, password: string) (fromAddress: string, toAddress: string, subject: string, body: string) =
    let mail = new MailMessage()
    mail.To.Add(toAddress)
    mail.Subject <- subject
    mail.Body <- body
    mail.From <- new MailAddress(fromAddress)

    let smtpClient = new SmtpClient(smtpServer, port)
    smtpClient.EnableSsl <- ssl
    smtpClient.Credentials <- new System.Net.NetworkCredential(username, password)
    smtpClient.Send(mail)

let sendEmail(toAddress: string, subject: string, body: string) =
    sendEmailInternal ("in-v3.mailjet.com", 587, false, "a704cec2127522ea06907182eac9c1d8", "c3846ce79bda1327d91c4b9e2002254e") ("fppt8test@gmail.com", toAddress, subject, body)
