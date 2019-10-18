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
                    await Authenticate();
                    StartCommand("MoList");
                    response = await Server.RequestAsync(mo);
                    EndCommand();
                }
                catch (Exception)
                {
                    EndCommand(false);
                    throw;
                }

                Console.WriteLine(response.mediaObjects.Length, Color.Azure);
            }
            public MoListCmd(IConfiguration config, ILogger logger, AIS.Server server)
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
                    await Authenticate();
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
                    await Authenticate();
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
