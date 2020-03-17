using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Console = Colorful.Console;

namespace Celin
{
    [Subcommand(typeof(MoDelCmd))]
    [Subcommand(typeof(MoEditCmd))]
    [Subcommand(typeof(MoAddCmd))]
    [Subcommand(typeof(MoGetCmd))]
    [Subcommand(typeof(MoListCmd))]
    [Subcommand(typeof(MoDownloadCmd))]
    [Subcommand(typeof(MoUploadCmd))]
    public class MoCmd
    {
        [Command("list", Description = "List Attachments")]
        class MoListCmd : MoBaseCmd<AIS.MoList>
        {
            [Option("-iu|--includeUrl", CommandOptionType.SingleOrNoValue, Description = "Include Url's")]
            bool IncludeUrls { get; set; }
            [Option("-id|-includeData", CommandOptionType.SingleOrNoValue, Description = "Include Data")]
            bool IncludeData { get; set; }
            [Option("-ts|--thumbsize", CommandOptionType.SingleOrNoValue, Description = "Thumbnail Size")]
            int ThumbNailSize { get; set; }
            [Option("-mt|--motypes", CommandOptionType.SingleOrNoValue, Description = "Mo Types (separate multiple values with ;)")]
            string MoTypes { get; set; }
            async Task OnExecuteAsync()
            {
                var mo = GetMoRequest();
                mo.includeURLs = IncludeUrls;
                mo.includeData = IncludeData;
                mo.moTypes = MoTypes?.Split(";");

                AIS.AttachmentListResponse response;
                try
                {
                    Authenticate();
                    StartCommand("MoList");
                    response = await Server.RequestAsync(mo);
                    EndCommand();
                }
                catch (Exception)
                {
                    EndCommand(false);
                    throw;
                }

                Console.WriteLine(string.Format("{0} has {1} attachments:", MoStructure, response.mediaObjects.Length), Color.Azure);
                foreach (var a in response.mediaObjects)
                {
                    Console.WriteLine(string.Format("{0}\t{1}", a.sequence, a.itemName), Color.Azure);
                }
            }
            public MoListCmd(IConfiguration config, ILogger logger, AIS.Server server)
                : base(config, logger, server) { }
        }
        [Command("get", Description = "Get Text Attachment")]
        class MoGetCmd : MoBaseCmd<AIS.MoGetText>
        {
            [Argument(3, Description = "Mo Sequence Number")]
            int? Sequence { get; set; }
            async Task OnExecuteAsync()
            {
                var mo = GetMoRequest();
                mo.multipleMode = true;
                mo.sequence = Sequence;

                AIS.AttachmentResponse response;
                try
                {
                    Authenticate();
                    StartCommand("MoGet");
                    response = await Server.RequestAsync(mo);
                    EndCommand();
                }
                catch (Exception)
                {
                    EndCommand(false);
                    throw;
                }

                foreach (var a in response.textAttachments)
                {
                    Console.WriteLine(string.Format("{0}\t{1}", a.itemName, a.text), Color.Azure);
                }
            }
            public MoGetCmd(IConfiguration config, ILogger logger, AIS.Server server)
                : base(config, logger, server) { }
        }
        [Command("add", Description = "Add Text Attachment")]
        class MoAddCmd : MoBaseCmd<AIS.MoAddText>
        {
            [Argument(3, Description = "Text")]
            [Required]
            public string Text { get; set; }
            [Option("-n|--name", CommandOptionType.SingleValue, Description = "Name")]
            public string Name { get; set; }
            async Task OnExecuteAsync()
            {
                var mo = GetMoRequest();
                mo.itemName = Name;
                mo.inputText = Text;
                AIS.AttachmentResponse response;
                try
                {
                    Authenticate();
                    StartCommand("MoAdd");
                    response = await Server.RequestAsync(mo);
                    EndCommand();
                }
                catch (Exception)
                {
                    EndCommand(false);
                    throw;
                }
                Console.WriteLine("{0}, Sequence {1}", response.addTextStatus, response.sequence);
            }
            public MoAddCmd(IConfiguration config, ILogger logger, AIS.Server server)
                : base(config, logger, server) { }
        }
        [Command("edit", Description = "Edit text attachment")]
        class MoEditCmd : MoBaseCmd<AIS.MoUpdateText>
        {
            [Argument(3, Description = "Sequence Number")]
            [Required]
            int Sequence { get; set; }
            [Argument(4, Description = "Text")]
            [Required]
            public string Text { get; set; }
            [Option("-o|--override", CommandOptionType.NoValue, Description = "Override existing text")]
            bool Override { get; set; }
            async Task OnExecuteAsync()
            {
                var mo = GetMoRequest();
                mo.inputText = Text;
                mo.sequence = Sequence;
                mo.appendText = !Override;
                AIS.AttachmentResponse response;
                try
                {
                    Authenticate();
                    StartCommand("MoEdit");
                    response = await Server.RequestAsync(mo);
                    EndCommand();
                }
                catch (Exception)
                {
                    EndCommand(false);
                    throw;
                }
                Console.WriteLine("{0}", response.updateTextStatus);
            }

            public MoEditCmd(IConfiguration config, ILogger logger, AIS.Server server)
                : base(config, logger, server) { }
        }
        [Command("del", Description = "Delete Attachment")]
        class MoDelCmd : MoBaseCmd<AIS.MoDelete>
        {
            [Argument(3, Description = "Sequence Number")]
            [Required]
            int Sequence { get; set; }
            async Task OnExecuteAsync()
            {
                var mo = GetMoRequest();
                mo.sequence = Sequence;
                AIS.AttachmentResponse response;
                try
                {
                    Authenticate();
                    StartCommand("MoDel");
                    response = await Server.RequestAsync(mo);
                    EndCommand();
                }
                catch (Exception)
                {
                    EndCommand(false);
                    throw;
                }
                Console.WriteLine("{0}", response.deleteStatus);
            }
            public MoDelCmd(IConfiguration config, ILogger logger, AIS.Server server)
                : base(config, logger, server) { }
        }
        [Command("download", Description = "Download File Attachment")]
        class MoDownloadCmd : MoBaseCmd<AIS.MoDownload>
        {
            [Argument(3, Description = "Mo Sequence Number")]
            [Required]
            int Sequence { get; set; }
            [Argument(4, Description = "Output File Name")]
            [Required]
            string FileName { get; set; }
            async Task OnExecuteAsync()
            {
                var mo = GetMoRequest();
                mo.sequence = Sequence;
                Stream response;
                try
                {
                    Authenticate();
                    StartCommand("MoDownload");
                    response = await Server.RequestAsync(mo);
                    EndCommand();
                }
                catch (Exception)
                {
                    EndCommand(false);
                    throw;
                }
                var file = File.OpenWrite(FileName);
                response.CopyTo(file);
            }
            public MoDownloadCmd(IConfiguration config, ILogger logger, AIS.Server server)
                : base(config, logger, server) { }
        }
        [Command("upload", Description = "Upload File Attachment")]
        class MoUploadCmd : MoBaseCmd<AIS.MoUpload>
        {
            [Argument(3, Description = "Item Name")]
            [Required]
            string ItemName { get; set; }
            [Argument(4, Description = "Upload File Name")]
            [Required]
            string FileName { get; set; }
            async Task OnExecuteAsync()
            {
                Stream stream = File.OpenRead(FileName);
                var mo = GetMoRequest();
                AIS.FileAttachmentResponse response;
                mo.file = new AIS.FileAttachment
                {
                    itemName = ItemName,
                    fileName = FileName
                };
                try
                {
                    Authenticate();
                    StartCommand("MoUpload");
                    response = await Server.RequestAsync(mo, new StreamContent(stream));
                    EndCommand();
                }
                catch (Exception)
                {
                    EndCommand(false);
                    throw;
                }
                Console.WriteLine("Sequence {0}", response.sequence);
            }
            public MoUploadCmd(IConfiguration config, ILogger logger, AIS.Server server)
                : base(config, logger, server) { }
        }
    }
}
