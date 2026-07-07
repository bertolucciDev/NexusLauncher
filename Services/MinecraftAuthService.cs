using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using NexusLauncher.Models;
using System;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NexusLauncher.Services;

[SupportedOSPlatform("windows")]
public class MinecraftAuthService
{
    private const string ClientId = "00000000402B5328";
    private const string RedirectUri = "https://login.live.com/oauth20_desktop.srf";
    private const string AuthorizeEndpoint = "https://login.live.com/oauth20_authorize.srf";
    private const string XboxAuth = "https://user.auth.xboxlive.com/user/authenticate";
    private const string XstsAuth = "https://xsts.auth.xboxlive.com/xsts/authorize";
    private const string MinecraftLogin = "https://api.minecraftservices.com/authentication/login_with_xbox";
    private const string MinecraftProfile = "https://api.minecraftservices.com/minecraft/profile";

    private readonly HttpClient _http;

    public MinecraftAuthService(HttpClient http)
    {
        _http = http;
    }

    public async Task<MinecraftAccount> LoginMicrosoftAsync(
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report("Abrindo janela de autenticacao...");

        var msAccessToken = await GetMicrosoftTokenAsync(ct);
        if (string.IsNullOrEmpty(msAccessToken))
            throw new OperationCanceledException("Login cancelado ou falhou.");

        progress?.Report("Autenticando Xbox Live...");
        var xbl = await AuthenticateXboxAsync(msAccessToken, ct);

        progress?.Report("Autorizando XSTS...");
        var xsts = await AuthorizeXstsAsync(xbl.Token, ct);

        progress?.Report("Conectando ao Minecraft...");
        var mc = await LoginMinecraftAsync(xsts.Token, xbl.UserHash, ct);

        progress?.Report("Obtendo perfil...");
        var profile = await GetMinecraftProfileAsync(mc.AccessToken, ct);

        return new MinecraftAccount
        {
            Type = "microsoft",
            Username = profile.Username,
            Uuid = profile.Uuid,
            MinecraftAccessToken = mc.AccessToken
        };
    }

    public async Task<MinecraftAccount> RefreshAsync(CancellationToken ct = default)
    {
        var msAccessToken = await GetMicrosoftTokenAsync(ct);
        if (string.IsNullOrEmpty(msAccessToken))
            throw new InvalidOperationException("Falha ao renovar sessao Microsoft.");

        var xbl = await AuthenticateXboxAsync(msAccessToken, ct);
        var xsts = await AuthorizeXstsAsync(xbl.Token, ct);
        var mc = await LoginMinecraftAsync(xsts.Token, xbl.UserHash, ct);
        var profile = await GetMinecraftProfileAsync(mc.AccessToken, ct);

        return new MinecraftAccount
        {
            Type = "microsoft",
            Username = profile.Username,
            Uuid = profile.Uuid,
            MinecraftAccessToken = mc.AccessToken
        };
    }

    private async Task<string?> GetMicrosoftTokenAsync(CancellationToken ct)
    {
        var tcs = new TaskCompletionSource<string?>();
        Form? formRef = null;
        ct.Register(() =>
        {
            tcs.TrySetCanceled();
            formRef?.BeginInvoke(() => formRef.Close());
        });

        var thread = new Thread(() =>
        {
            try
            {
                var form = new Form
                {
                    Width = 600,
                    Height = 700,
                    StartPosition = FormStartPosition.CenterScreen,
                    Text = "Nexus Launcher - Login Microsoft",
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                formRef = form;

                var webView = new WebView2 { Dock = DockStyle.Fill };
                form.Controls.Add(webView);
                form.FormClosed += (_, _) => tcs.TrySetResult(null);

                form.Load += async (_, _) =>
                {
                    try
                    {
                        var env = await CoreWebView2Environment.CreateAsync();
                        await webView.EnsureCoreWebView2Async(env);

                        webView.CoreWebView2.NavigationStarting += (_, args) =>
                        {
                            if (args.Uri.StartsWith(RedirectUri, StringComparison.OrdinalIgnoreCase))
                            {
                                var uri = new Uri(args.Uri);
                                var fragment = uri.Fragment.TrimStart('#');
                                if (!string.IsNullOrEmpty(fragment))
                                {
                                    foreach (var pair in fragment.Split('&'))
                                    {
                                        var parts = pair.Split('=', 2);
                                        if (parts.Length == 2 && parts[0] == "access_token")
                                        {
                                            tcs.TrySetResult(parts[1]);
                                            form.BeginInvoke(() => form.Close());
                                            return;
                                        }
                                    }
                                }
                                tcs.TrySetResult(null);
                                form.BeginInvoke(() => form.Close());
                            }
                        };

                        webView.CoreWebView2.NewWindowRequested += (_, args) => args.Handled = true;

                        var authorizeUrl = $"{AuthorizeEndpoint}?client_id={ClientId}" +
                            $"&response_type=token&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                            $"&scope=XboxLive.signin+offline_access&display=touch";

                        webView.CoreWebView2.Navigate(authorizeUrl);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                        form.BeginInvoke(() => form.Close());
                    }
                };

                Application.Run(form);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return await tcs.Task;
    }

    private async Task<(string Token, string UserHash)> AuthenticateXboxAsync(string accessToken, CancellationToken ct)
    {
        var payload = new
        {
            Properties = new
            {
                AuthMethod = "RPS",
                SiteName = "user.auth.xboxlive.com",
                RpsTicket = $"d={accessToken}"
            },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        };

        var resp = await _http.PostAsync(XboxAuth, new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"), ct);
        var body = await EnsureSuccessAsync(resp, "Falha na autenticacao Xbox Live");

        using var doc = JsonDocument.Parse(body);
        return (
            doc.RootElement.GetProperty("Token").GetString()!,
            doc.RootElement.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString()!
        );
    }

    private async Task<(string Token, string UserHash)> AuthorizeXstsAsync(string xblToken, CancellationToken ct)
    {
        var payload = new
        {
            Properties = new
            {
                SandboxId = "RETAIL",
                UserTokens = new[] { xblToken }
            },
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT"
        };

        var resp = await _http.PostAsync(XstsAuth, new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"), ct);
        var body = await EnsureSuccessAsync(resp, "Falha na autorizacao XSTS");

        using var doc = JsonDocument.Parse(body);
        return (
            doc.RootElement.GetProperty("Token").GetString()!,
            doc.RootElement.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString()!
        );
    }

    private async Task<(string AccessToken, string Username)> LoginMinecraftAsync(string xstsToken, string userHash, CancellationToken ct)
    {
        var identityToken = $"XBL3.0 x={userHash};{xstsToken}";
        var payload = new { identityToken };

        var resp = await _http.PostAsync(MinecraftLogin, new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"), ct);
        var body = await EnsureSuccessAsync(resp, "Falha no login Minecraft");

        using var doc = JsonDocument.Parse(body);
        return (
            doc.RootElement.GetProperty("access_token").GetString()!,
            doc.RootElement.GetProperty("username").GetString() ?? string.Empty
        );
    }

    private async Task<(string Username, string Uuid)> GetMinecraftProfileAsync(string mcToken, CancellationToken ct)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, MinecraftProfile);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", mcToken);
        var resp = await _http.SendAsync(req, ct);
        var body = await EnsureSuccessAsync(resp, "Falha ao obter perfil Minecraft");

        using var doc = JsonDocument.Parse(body);
        return (
            doc.RootElement.GetProperty("name").GetString()!,
            doc.RootElement.GetProperty("id").GetString()!
        );
    }

    private static async Task<string> EnsureSuccessAsync(HttpResponseMessage resp, string context)
    {
        if (resp.IsSuccessStatusCode)
            return await resp.Content.ReadAsStringAsync();

        var body = await resp.Content.ReadAsStringAsync();
        var detail = TryExtractErrorDescription(body);
        throw new InvalidOperationException($"{context}: {detail ?? body}");
    }

    private static string? TryExtractErrorDescription(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("error_description", out var desc))
                return desc.GetString();
            if (doc.RootElement.TryGetProperty("error", out var err))
                return err.GetString();
        }
        catch { }
        return null;
    }
}