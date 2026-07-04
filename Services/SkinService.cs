using Avalonia.Media.Imaging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace NexusLauncher.Services;

public class SkinService
{
    private static readonly HttpClient HttpClient = new();

    public async Task<Bitmap?> GetAvatarAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return null;

        try
        {
            var safeName = Uri.EscapeDataString(username.Trim());
            var data = await HttpClient.GetByteArrayAsync($"https://minotar.net/avatar/{safeName}/128.png");
            return new Bitmap(new MemoryStream(data));
        }
        catch
        {
            return null;
        }
    }
}
