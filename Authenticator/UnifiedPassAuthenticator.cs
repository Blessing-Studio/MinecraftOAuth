﻿using MinecraftLaunch.Modules.Models.Auth;
using MinecraftLaunch.Modules.Utils;
using MinecraftOAuth.Module.Base;
using MinecraftOAuth.Module.Models;
using Natsurainko.Toolkits.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftOAuth.Authenticator {
    /// <summary>
    /// 统一通行证验证器    
    /// </summary>
    public class UnifiedPassAuthenticator : AuthenticatorBase {
        private static readonly string BaseApi = "https://auth.mc-user.com:233/";

        /// <summary>
        /// 统一通行证提供的 32 位服务器 Id
        /// </summary>
        public string ServerId { get; set; } = string.Empty;

        /// <summary>
        /// 统一通行证用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 统一通行证密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 统一通行证验证器    
        /// </summary>
        public UnifiedPassAuthenticator(string serverId, string userName, string passWord) { 
            ServerId = serverId;
            UserName = userName;
            Password = passWord;
        }

        /// <summary>
        /// 异步验证方法
        /// </summary>
        /// <param name="func"></param>
        /// <returns></returns>
        public new async ValueTask<UnifiedPassAccount> AuthAsync(Action<string> func = default!) {
            string authUrl = $"{BaseApi}{ServerId}/authserver/authenticate";
            var content = new {
                agent = new {
                    name = "MinecraftLaunch",
                    version = 1.00
                },
                username = UserName,
                password = Password,
                clientToken = string.Empty,
                requestUser = true,
            }.ToJson();

            using var httpResponse = await HttpWrapper.HttpPostAsync(authUrl, json);
            string userDataJson = await httpResponse.Content.ReadAsStringAsync();
            var model = userDataJson.ToJsonEntity<YggdrasilResponse>();

            var user = model.UserAccounts.FirstOrDefault();
            return new UnifiedPassAccount() {
                AccessToken = model.AccessToken,
                ClientToken = model.ClientToken,
                Name = user!.Name,
                Uuid = Guid.Parse(user.Uuid),
                ServerId = ServerId,
            };
        }

        /// <summary>
        /// 异步刷新方法
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public async ValueTask<UnifiedPassAccount> RefreshAsync(UnifiedPassAccount account) {
            var content = new {
                accessToken = account.AccessToken,
                clientToken = account.ClientToken,
                requestUser = true
            }.ToJson();

            using var responseMessage = await HttpWrapper.HttpPostAsync($"{BaseApi}{ServerId}/authserver/refresh", content);
            string json = await responseMessage.Content.ReadAsStringAsync();

            YggdrasilResponse model = json.ToJsonEntity<YggdrasilResponse>();
            var user = model.UserAccounts.FirstOrDefault();

            return new UnifiedPassAccount() {
                AccessToken = model.AccessToken,
                ClientToken = model.ClientToken,
                Name = user!.Name,
                Uuid = Guid.Parse(user.Uuid),
                ServerId = ServerId,
            };
        }

        public async ValueTask<bool> ValidateAsync(UnifiedPassAccount account) {
            string url = $"{BaseApi}{ServerId}/authserver/validate";
            var content = new {
                accessToken = account.AccessToken,
                clientToken = account.ClientToken,
            }.ToJson();

            using var responseMessage = await HttpWrapper.HttpPostAsync(url, content);
            return responseMessage.IsSuccessStatusCode;
        }

        public async ValueTask<bool> SignoutAsync() {
            string url = $"{BaseApi}{ServerId}/authserver/signout";
            var content = new {
                username = UserName,
                password = Password,
            }.ToJson();

            using var responseMessage = await HttpWrapper.HttpPostAsync(url, content);
            return responseMessage.IsSuccessStatusCode;
        }
    }
}
