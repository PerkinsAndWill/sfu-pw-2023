using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DPredict;

namespace Servers
{
    class DataLoadServer
    {

        internal static bool isServerRunning = false;
        internal static async Task StartServer()
        {
            string url = "http://localhost:3000/";
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine($"Server running at {url}");
            isServerRunning = true;
            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                Task.Run(() => HandleRequest(context));
            }
        }

        static async Task HandleRequest(HttpListenerContext context)
        {
            var baseDir = UnicornPlugin.Instance.GetDataFolderPath();
            string filePath = Path.Combine(baseDir, context.Request.Url.LocalPath.TrimStart('/'));
            string fileExtension = Path.GetExtension(filePath)?.ToLower();
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

            if (Array.Exists(allowedExtensions, ext => ext == fileExtension))
            {
                if (File.Exists(filePath))
                {
                    try
                    {
                        byte[] buffer = File.ReadAllBytes(filePath);
                        context.Response.ContentType = $"image/{fileExtension.Substring(1)}";
                        context.Response.ContentLength64 = buffer.Length;
                        await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                    }
                    catch
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        byte[] errorMessage = Encoding.UTF8.GetBytes("Internal Server Error");
                        await context.Response.OutputStream.WriteAsync(errorMessage, 0, errorMessage.Length);
                    }
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    byte[] errorMessage = Encoding.UTF8.GetBytes("File Not Found");
                    await context.Response.OutputStream.WriteAsync(errorMessage, 0, errorMessage.Length);
                }
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                byte[] errorMessage = Encoding.UTF8.GetBytes("Bad Request");
                await context.Response.OutputStream.WriteAsync(errorMessage, 0, errorMessage.Length);
            }

            context.Response.OutputStream.Close();
        }

    }
}
