// See https://aka.ms/new-console-template for more information
// Console.WriteLine("Hello, World!");
//此处只是单纯展示一下能够正确发送的代码，emailservice中完全按照该代码进行书写
using System;
using System.Net;
using System.Net.Mail;

class Program
{
static void Main(string[] args)
{
// 创建邮件消息
MailMessage mail = new MailMessage();
mail.From = new MailAddress("1870707155@qq.com"); // 发件人邮箱
mail.To.Add("1591503159@qq.com"); // 收件人邮箱
mail.Subject = "测试邮件"; // 邮件主题
mail.Body = "这是通过C#发送的测试邮件！"; // 邮件内容
mail.IsBodyHtml = false; // 是否为HTML格式

// 配置SMTP客户端
SmtpClient smtp = new SmtpClient("smtp.qq.com", 587); // QQ邮箱SMTP服务器地址和端口号
smtp.Credentials = new NetworkCredential("1870707155@qq.com", "gsqlerqxaryaefcf"); // 邮箱账号和授权码
smtp.EnableSsl = true; // 启用SSL加密

try
{
smtp.Send(mail); // 发送邮件
Console.WriteLine("邮件发送成功！");
}
catch (Exception ex)
{
Console.WriteLine($"邮件发送失败：{ex.Message}");
}
}
}