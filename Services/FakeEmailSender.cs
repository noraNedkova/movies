using Microsoft.AspNetCore.Identity.UI.Services;

namespace Movies.Services;

public class FakeEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage) =>
        Task.CompletedTask;
}
