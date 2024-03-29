﻿using Flurl.Http;
using System.Text.Json.Nodes;

namespace MinecraftLaunch.Modules.Models.Auth {
    /// <summary>
    /// 微软账户的基本操作扩展
    /// </summary>
    public static class MicrosoftAccountExtend {
        private const string NameChangeAPI = "https://api.minecraftservices.com/minecraft/profile/name/";

        private const string UploadSkinAPI = "https://api.minecraftservices.com/minecraft/profile/skins";

        private const string CapeAPI = "https://api.minecraftservices.com/minecraft/profile/capes/active";

        private const string ResetSkinAPI = "https://api.minecraftservices.com/minecraft/profile/skins/active";

        /// <summary>
        /// 异步上传皮肤至 Mojang 服务器
        /// </summary>
        /// <param name="path">皮肤文件地址</param>
        public static async ValueTask<bool> SkinUploadAsync(this MicrosoftAccount account, string path) {
            if (File.Exists(path)) {
                var result = await UploadSkinAPI.AllowAnyHttpStatus()
                    .WithOAuthBearerToken(account.AccessToken).PostMultipartAsync(content =>
                        content.AddString("variant", "slim")
                            .AddFile("file", path));
                var json = await result.GetStringAsync();

                if (!string.IsNullOrEmpty(json)) {
                    JsonNode model = JsonNode.Parse(json)!;
                    if (account.Uuid.ToString() == model["id"]!.GetValue<string>() && account.Name == model["name"]!.GetValue<string>()) {
                        return true;
                    }
                }
            } else {
                throw new Exception("错误：此皮肤路径不存在！");
            }

            return false;
        }

        /// <summary>
        /// 异步上传皮肤至 Mojang 服务器
        /// </summary>
        /// <param name="path">皮肤文件地址</param>
        public static async ValueTask<bool> SkinUploadAsync(this MicrosoftAccount account, FileInfo path) {
            var result = await UploadSkinAPI.AllowAnyHttpStatus()
                .WithOAuthBearerToken(account.AccessToken).PostMultipartAsync(content =>
                    content.AddString("variant", "slim")
                    .AddFile("file", path.FullName));

            var json = await result.GetStringAsync();

            if (!string.IsNullOrEmpty(json)) {
                JsonNode model = JsonNode.Parse(json)!;
                if (account.Uuid.ToString() == model["id"]!.GetValue<string>() && account.Name == model["name"]!.GetValue<string>()) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 删除当前启用的皮肤并重置为默认皮肤
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public static async ValueTask<bool> SkinResetAsync(this MicrosoftAccount account) {
            var result = await ResetSkinAPI.AllowAnyHttpStatus()
                .WithOAuthBearerToken(account.AccessToken).DeleteAsync();

            var json = await result.GetStringAsync();

            if (!string.IsNullOrEmpty(json)) {
                JsonNode model = JsonNode.Parse(json)!;
                if (account.Uuid.ToString() == model["id"]!.GetValue<string>() && account.Name == model["name"]!.GetValue<string>()) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 异步隐藏皮肤的披风（如果有）
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public static async ValueTask<bool> CapeHideAsync(this MicrosoftAccount account) {
            var result = await CapeAPI.AllowAnyHttpStatus()
                .WithOAuthBearerToken(account.AccessToken)
                .DeleteAsync();

            var json = await result.GetStringAsync();
            if (!string.IsNullOrEmpty(json)) {
                JsonNode model = JsonNode.Parse(json)!;
                if (account.Uuid.ToString() == model["id"]!.GetValue<string>() && account.Name == model["name"]!.GetValue<string>()) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 异步显示隐藏的披风（如果有）
        /// </summary>
        /// <param name="capeId">披风 Id</param>
        /// <returns></returns>
        public static async ValueTask<bool> ShowCapeAsync(this MicrosoftAccount account, string capeId) {
            var content = new {
                capeId = capeId
            };
            var result = await CapeAPI.AllowAnyHttpStatus()
                .WithOAuthBearerToken(account.AccessToken)
                .PutJsonAsync(content);

            var json = await result.GetStringAsync();
            if (!string.IsNullOrEmpty(json)) {
                JsonNode model = JsonNode.Parse(json)!;
                if (account.Uuid.ToString() == model["id"]!.GetValue<string>() && account.Name == model["name"]!.GetValue<string>()) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 异步更改游戏内用户名
        /// </summary>
        /// <param name="newName">新的用户名</param>
        /// <returns></returns>
        public static async ValueTask<bool> UsernameChangeAsync(this MicrosoftAccount account, string newName) {
            var fullUrl = $"{NameChangeAPI}{newName}";
            var result = await fullUrl.WithOAuthBearerToken(account.AccessToken)
                .PutAsync();

            if (result.StatusCode is 200) {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检查是否可以更改为某个名称
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public static async ValueTask<bool> CheckNameIsUsableAsync(this MicrosoftAccount account, string newName) {
            var fullUrl = $"{NameChangeAPI}{newName}/available";
            var json = await fullUrl.WithOAuthBearerToken(account.AccessToken)
                .GetStringAsync();

            if (!string.IsNullOrEmpty(json)) {
                JsonNode model = JsonNode.Parse(json)!;
                if (model["status"]!.GetValue<string>().Contains("NOT")) {
                    return false;
                }
            } else {
                return false;
            }

            return true;
        }
    }
}
