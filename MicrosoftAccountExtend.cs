using Flurl.Http;
using Newtonsoft.Json.Linq;
using System.Net;

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
        /// 删除当前启用的皮肤并重置为默认皮肤
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public static async ValueTask<bool> SkinResetAsync(this MicrosoftAccount account)
        {
            var res = await ResetSkinAPI.AllowAnyHttpStatus()
            .WithOAuthBearerToken(account.AccessToken).DeleteAsync();

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
        /// 异步隐藏皮肤的披风（如果有）
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public static async ValueTask<bool> CapeHideAsync(this MicrosoftAccount account)
        {
            var res = await CapeAPI.AllowAnyHttpStatus().WithOAuthBearerToken(account.AccessToken).DeleteAsync();

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
        /// 异步显示隐藏的披风（如果有）
        /// </summary>
        /// <param name="capeId">披风 Id</param>
        /// <returns></returns>
        public static async ValueTask<bool> ShowCapeAsync(this MicrosoftAccount account, string capeId)
        {
            var content = new
            {
                capeId = capeId
            };
            var res = await CapeAPI.AllowAnyHttpStatus().WithOAuthBearerToken(account.AccessToken).PutJsonAsync(content);

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
        /// 异步更改游戏内用户名
        /// </summary>
        /// <param name="newName">新的用户名</param>
        /// <returns></returns>
        public static async ValueTask<bool> UsernameChangeAsync(this MicrosoftAccount account, string newName)
        {
            var fullUrl = $"{NameChangeAPI}{newName}";
            var code = (await fullUrl.WithOAuthBearerToken(account.AccessToken).PutAsync()).StatusCode;

            if (code is 200)
                return true;

            return false;
        }

        /// <summary>
        /// 检查是否可以更改为某个名称
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public static async ValueTask<bool> CheckNameIsUsableAsync(this MicrosoftAccount account, string newName)
        {
            var fullUrl = $"{NameChangeAPI}{newName}/available";
            var res = (await fullUrl.WithOAuthBearerToken(account.AccessToken).GetStringAsync());

            if (!string.IsNullOrEmpty(res))
            {
                JObject objects = new(res);
                if (objects["status"]!.ToString().Contains("NOT"))
                    return false;
            }
            else return false;

            return true;
        }
    }

    partial class MicrosoftAccountExtend
    {
        const string NameChangeAPI = "https://api.minecraftservices.com/minecraft/profile/name/";
        const string UploadSkinAPI = "https://api.minecraftservices.com/minecraft/profile/skins";
        const string ResetSkinAPI = "https://api.minecraftservices.com/minecraft/profile/skins/active";
        const string CapeAPI = "https://api.minecraftservices.com/minecraft/profile/capes/active";
    }
}
