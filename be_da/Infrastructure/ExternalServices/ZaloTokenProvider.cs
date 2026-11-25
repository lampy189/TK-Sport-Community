using System.Text.Json;
using be_da.Application.DTOs;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace be_da.Infrastructure.ExternalServices;

public sealed class ZaloTokenProvider(IHttpClientFactory clients,IDataProtectionProvider protection,IOptions<ZaloOptions> options)
{
    private sealed record Tokens(string Access,string Refresh,DateTimeOffset ExpiresAt);
    private readonly SemaphoreSlim gate=new(1,1);
    public async Task<string> GetAsync(CancellationToken ct)
    {
        var o=options.Value;
        if(string.IsNullOrWhiteSpace(o.RefreshToken))
            return !string.IsNullOrWhiteSpace(o.AccessToken) ? o.AccessToken : throw new InvalidOperationException("Thiếu token Zalo.");
        if(string.IsNullOrWhiteSpace(o.TokenFile) || !Path.IsPathFullyQualified(o.TokenFile) || string.IsNullOrWhiteSpace(o.AppSecret))
            throw new InvalidOperationException("Refresh Zalo cần AppSecret và đường dẫn tuyệt đối TokenFile ngoài thư mục public.");
        await gate.WaitAsync(ct);
        try
        {
            var path=Path.GetFullPath(o.TokenFile);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            // Shared file lock + the worker's SQL lock prevent rotating a single-use token twice.
            await using var fileLock=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            var protector=protection.CreateProtector("TKSC.ZaloTokens",o.AppId,o.OaId);
            Tokens? saved=File.Exists(path) ? JsonSerializer.Deserialize<Tokens>(protector.Unprotect(await File.ReadAllTextAsync(path,ct))) : null;
            if(saved is not null && saved.ExpiresAt>DateTimeOffset.UtcNow.AddMinutes(5)) return saved.Access;
            using var request=new HttpRequestMessage(HttpMethod.Post,"https://oauth.zaloapp.com/v4/oa/access_token");
            request.Headers.Add("secret_key",o.AppSecret);
            request.Content=new FormUrlEncodedContent(new Dictionary<string,string> {
                ["app_id"]=o.AppId,["grant_type"]="refresh_token",["refresh_token"]=saved?.Refresh??o.RefreshToken
            });
            using var response=await clients.CreateClient("zalo-token").SendAsync(request,ct);
            if(!response.IsSuccessStatusCode) throw new InvalidOperationException("Không thể làm mới token Zalo.");
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var access=json.RootElement.GetProperty("access_token").GetString();
            var refresh=json.RootElement.GetProperty("refresh_token").GetString();
            var expiry=json.RootElement.GetProperty("expires_in").ToString();
            if(string.IsNullOrWhiteSpace(access)||string.IsNullOrWhiteSpace(refresh)||!int.TryParse(expiry,out var seconds)||seconds<60)
                throw new InvalidOperationException("Phản hồi làm mới token Zalo không hợp lệ.");
            var next=new Tokens(access,refresh,DateTimeOffset.UtcNow.AddSeconds(seconds));
            await File.WriteAllTextAsync(path+".tmp",protector.Protect(JsonSerializer.Serialize(next)),ct);
            File.Move(path+".tmp",path,true);
            return next.Access;
        }
        finally {gate.Release();}
    }
}
