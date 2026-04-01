using Spectre.Console.Cli;
using Spectre.Console;
using System.ComponentModel;
using System.Net;
using System.Text;
using DoomedCLI.Utility.HTTP;
using DoomedCLI.Utility;

internal class LoginCommand : AsyncCommand<LoginCommand.Settings>
{

    internal record LoginRequest(string Name);

    private readonly HttpHandler _httpClientHandler = new(new HttpClient
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("HOST_IP")??"")
    }
    );

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[username]")]
        [Description("Username of the user to login")]
        public string UserName { get; init; } = string.Empty;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        // Start local listener FIRST
        var listener = new HttpListener();
        var callbackUri = $"{Environment.GetEnvironmentVariable("CALLBACK_URI")??""}callback/";

        listener.Prefixes.Add(callbackUri);
        listener.Start();

        // Open browser
        AnsiConsole.MarkupLine("[yellow]Opening browser for Google login...[/]");
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = $"{Environment.GetEnvironmentVariable("HOST_IP")??""}oauth2/authorization/google",
            UseShellExecute = true
        });

        // Wait for the callback
        AnsiConsole.MarkupLine("[grey]Waiting for login...[/]");
        var httpContext = await listener.GetContextAsync();

        // Extract session ID from query string
        var sessionId = httpContext.Request.QueryString["sessionId"];

        // Send a response to close the browser tab
        var responseString = "<html><body>Login successful! You can close this tab.</body></html>";
        var buffer = Encoding.UTF8.GetBytes(responseString);
        httpContext.Response.ContentLength64 = buffer.Length;
        await httpContext.Response.OutputStream.WriteAsync(buffer, cancellation);
        httpContext.Response.Close();
        listener.Stop();

        // Save session ID for future requests
        var token = httpContext.Request.QueryString["token"];
        File.WriteAllText(".token", token);
        File.WriteAllText(".email", httpContext.Request.QueryString["email"]);

        // Update the current username in the session
        await CurrentUser.GetUsernameAsync(cancellation, refresh: true);

        AnsiConsole.MarkupLine("[green]Login successful![/]");
        return 0;
    }
}