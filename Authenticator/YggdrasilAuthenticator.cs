using MinecaftOAuth.Module.Base;
using MinecaftOAuth.Module.Enum;
using MinecaftOAuth.Module.Models;
using MinecraftLaunch.Modules.Models.Auth;
using MinecraftLaunch.Modules.Toolkits;
using Natsurainko.Toolkits.Network;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Reflection;
using System.Xml.Linq;

namespace MinecaftOAuth.Authenticator
{
    /// <summary>
    /// 第三方验证器
    /// </summary>
    public partial class YggdrasilAuthenticator
    {
        public IEnumerable<YggdrasilAccount> Auth() => AuthAsync(null).Result;

        public async ValueTask<IEnumerable<YggdrasilAccount>> AuthAsync(Action<string> func)
        {
            #region
            IProgress<string> progress = new Progress<string>();
            ((Progress<string>)progress).ProgressChanged += ProgressChanged!;

            void ProgressChanged(object _, string e) =>
                func(e);

            void Report(string value)
            {
                if (func is not null)
                    progress.Report(value);
            }
            #endregion

            var ru = Uri;
            string content = string.Empty;
            Report("开始第三方（Yggdrasil）登录");
            var requestJson = new
            {
                clientToken = Guid.NewGuid().ToString("N"),
                username = Email,
                password = Password,
                requestUser = false,
                agent = new
                {
                    name = "Minecraft",
                    version = 1
                }
            }.ToJson();
            List<YggdrasilAccount> accounts = new();
            var res = await HttpWrapper.HttpPostAsync($"{Uri}{(string.IsNullOrEmpty(Uri) ? "https://authserver.mojang.com" : "/authserver")}/authenticate", requestJson);
            content = await res.Content.ReadAsStringAsync();
            foreach (var i in content.ToJsonEntity<YggdrasilResponse>().UserAccounts)
                accounts.Add(new YggdrasilAccount()
                {
                    AccessToken = content.ToJsonEntity<YggdrasilResponse>().AccessToken,
                    ClientToken = content.ToJsonEntity<YggdrasilResponse>().ClientToken,
                    Name = i.Name,
                    Uuid = Guid.Parse(i.Uuid),
                    YggdrasilServerUrl = this.Uri,
                    Email = this.Email,
                    Password = this.Password
                });
            return accounts;
        }

        /// <summary>
        /// 异步验证方法
        /// </summary>
        /// <param name="accesstoken"></param>
        /// <returns></returns>
        public async ValueTask<bool> ValidateAsync(string accesstoken)
        {
            var content = new
            {
                clientToken = ClientToken,
                accesstoken = accesstoken
            }.ToJson();
            using var res = await HttpWrapper.HttpPostAsync($"{Uri}/authserver/validate", content);
            return res.IsSuccessStatusCode;
        }
    }

    partial class YggdrasilAuthenticator
    {
        public YggdrasilAuthenticator() { }

        /// <summary>
        /// 标准第三方验证器构造方法
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="email"></param>
        /// <param name="password"></param>
        public YggdrasilAuthenticator(string uri, string email, string password)
        {
            Uri = uri;
            Email = email;
            Password = password;
        }

        /// <summary>
        /// LittleSkin验证器接口
        /// </summary>
        /// <param name="IsLittleSkin"></param>
        /// <param name="email"></param>
        /// <param name="password"></param>
        public YggdrasilAuthenticator(bool IsLittleSkin, string email, string password)
        {
            if (IsLittleSkin)
                Uri = "https://littleskin.cn/api/yggdrasil";
            Email = email;
            Password = password;
        }

        /// <summary>
        /// Mojang验证构造器（已弃用）
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>                
        [Obsolete] public YggdrasilAuthenticator(string email, string password) { }
    }

    partial class YggdrasilAuthenticator
    {
        public string? Uri { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string ClientToken { get; set; } = Guid.NewGuid().ToString("N");
        public string AccessToken { get; set; } = string.Empty;
    }

    internal class Model
    {
        public string username { get; set; } = "";
        public string password { get; set; } = "";
    }
}