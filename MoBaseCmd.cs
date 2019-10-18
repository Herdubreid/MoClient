using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Console = Colorful.Console;

namespace Celin
{
    abstract public class MoBaseCmd<T>
        where T : AIS.MoRequest, new()
    {
        protected ILogger Logger { get; }
        protected AIS.Server Server { get; }
        protected Stopwatch Stopwatch = new Stopwatch();

        [Option("-u|--user", CommandOptionType.SingleValue, Description = "User")]
        protected string User { get; set; }
        [Option("-p|--password", CommandOptionType.SingleOrNoValue, Description = "Password")]
        protected string Password { get; set; }
        [Option("-f|--form", CommandOptionType.SingleOrNoValue, Description = "Form Name")]
        protected string FormName { get; set; }
        [Option("-v|--Version", CommandOptionType.SingleOrNoValue, Description = "Version")]
        protected string Version { get; set; }
        [Argument(0, Description = "Mo Structure")]
        [Required]
        protected string MoStructure { get; set; }
        [Argument(1, Description = "Mo Keys (separate multiple values with ;)")]
        [Required]
        protected string MoKey { get; set; }

        protected void StartCommand(string name)
        {
            Console.Write($"{name}...", Color.Green);
            Stopwatch.Restart();
        }
        protected void EndCommand(bool success = true)
        {
            Stopwatch.Stop();
            Console.WriteLine(string.Format("{0} ({1}ms)", success ? "Done" : "Failed!", Stopwatch.ElapsedMilliseconds), success ? Color.Green : Color.Red);
        }
        protected T GetMoRequest()
        {
            var mo = new T();
            mo.formName = FormName ?? "FORM";
            mo.version = Version;
            mo.moStructure = MoStructure;
            mo.moKey = MoKey.Split(";");
            return mo;
        }
        protected async Task Authenticate()
        {
            if (User == null) User = Prompt.GetString("User:");
            if (Password == null) Password = Prompt.GetPassword("Password:");
            Server.AuthRequest.username = User;
            Server.AuthRequest.password = Password;

            StartCommand("Authenticate");
            await Server.AuthenticateAsync();
            EndCommand();
        }
        protected async Task LogOut()
        {
            StartCommand("Log Out");
            await Server.LogoutAsync();
            EndCommand();
        }
        public MoBaseCmd(IConfiguration config, ILogger logger, AIS.Server server)
        {
            Logger = logger;
            Server = server;
            User = config["User"];
            Password = config["Password"];
        }
    }
}
