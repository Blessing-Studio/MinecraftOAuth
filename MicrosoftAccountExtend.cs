using Flurl.Http;
using Natsurainko.Toolkits.Network;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftLaunch.Modules.Models.Auth
{
    public static partial class MicrosoftAccountExtend
    {
        /// <summary>
        /// 异步上传皮肤至 Mojang 服务器
        /// </summary>
        /// <param name="path">皮肤文件地址</param>
        public static async ValueTask<bool> SkinUploadAsync(this MicrosoftAccount account, string path)
        {
            if (File.Exists(path))
            {
                var res = await UploadSkinAPI.AllowAnyHttpStatus()
                    .WithOAuthBearerToken(account.AccessToken).PostMultipartAsync(content =>
                        content.AddString("variant", "slim")
                            .AddFile("file", path));

                var json = await res.GetStringAsync();

                if (!string.IsNullOrEmpty(json))
                {
                    JObject model = new(json);
                    if (account.Uuid.ToString() == model["id"]!.ToString() && account.Name == model["name"]!.ToString())
                        return true;
                }
            }
            else throw new Exception("错误：此皮肤路径不存在！");
            return false;
        }

        /// <summary>
        /// 异步上传皮肤至 Mojang 服务器
        /// </summary>
        /// <param name="path">皮肤文件地址</param>
        public static async ValueTask<bool> SkinUploadAsync(this MicrosoftAccount account, FileInfo path)
        {
            var res = await UploadSkinAPI.AllowAnyHttpStatus()
                .WithOAuthBearerToken(account.AccessToken).PostMultipartAsync(content =>
                    content.AddString("variant", "slim")
                        .AddFile("file", path.FullName));

            var json = await res.GetStringAsync();

            if (!string.IsNullOrEmpty(json))
            {
                JObject model = new(json);
                if (account.Uuid.ToString() == model["id"]!.ToString() && account.Name == model["name"]!.ToString())
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 删除当前启用的皮肤并重置为 Steve
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public static async ValueTask ResetSkinAsync(this MicrosoftAccount account)
        {
            var response = await ResetSkinAPI.AllowAnyHttpStatus()
            .WithOAuthBearerToken(account.AccessToken).DeleteAsync();

            var json = await response.GetStringAsync();
            Console.WriteLine(json);
        }
    }

    partial class MicrosoftAccountExtend
    {
        const string UploadSkinAPI = "https://api.minecraftservices.com/minecraft/profile/skins";
        const string ResetSkinAPI = "https://api.minecraftservices.com/minecraft/profile/skins/active";
    }
}
