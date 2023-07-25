using System.Diagnostics;
using System.Net;
using MinecraftOAuth.Models;
using MinecraftOAuth.Module.Base;
using MinecraftOAuth.Module.Enum;
using MinecraftOAuth.Module.Models;
using MinecraftLaunch.Modules.Enum;
using MinecraftLaunch.Modules.Models.Auth;
using MinecraftLaunch.Modules.Toolkits;
using Natsurainko.Toolkits.Network;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MinecraftOAuth.Authenticator {
    /// <summary>
    /// 微软验证器
    /// </summary>
    public partial class MicrosoftAuthenticator : AuthenticatorBase {
        /// <summary>
        /// 获取一次性验证代码
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public async ValueTask<DeviceCodeResponse> GetDeviceInfo() {
            if (string.IsNullOrEmpty(ClientId))
                throw new ArgumentNullException("ClientId为空！");

            //开始获取一次性验证代码
            using (var client = new HttpClient()) {
                string tenant = "/consumers";
                var content = new FormUrlEncodedContent(new Dictionary<string, string> {
                    ["client_id"] = ClientId,
                    ["tenant"] = tenant,
                    ["scope"] = string.Join(" ", Scopes)
                });

                var req = new HttpRequestMessage(HttpMethod.Post, "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode");
                req.Content = content;
                var res = await client.SendAsync(req);

                string json = await res.Content.ReadAsStringAsync();
                var codeResponse = JsonConvert.DeserializeObject<DeviceCodeResponse>(json);
                return codeResponse;
            }
        }

        /// <summary>
        /// 轮询获取令牌信息
        /// </summary>
        /// <returns></returns>
        public async ValueTask<TokenResponse> GetTokenResponse(DeviceCodeResponse codeResponse) {
            using (HttpClient client = new()) {
                //开始轮询
                string tenant = "/consumers";
                TimeSpan pollingInterval = TimeSpan.FromSeconds(codeResponse.Interval);
                DateTimeOffset codeExpiresOn = DateTimeOffset.UtcNow.AddSeconds(codeResponse.ExpiresIn);
                TimeSpan timeRemaining = codeExpiresOn - DateTimeOffset.UtcNow;
                TokenResponse tokenResponse = default!;

                while (timeRemaining.TotalSeconds > 0) {
                    var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://login.microsoftonline.com/consumers/oauth2/v2.0/token") {
                        Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                            ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code",
                            ["device_code"] = codeResponse.DeviceCode,
                            ["client_id"] = ClientId,
                            ["tenant"] = tenant
                        })
                    };

                    var tokenRes = await client.SendAsync(tokenRequest);

                    string tokenJson = await tokenRes.Content.ReadAsStringAsync();

                    var tempTokenResponse = JsonConvert.DeserializeObject<TokenResponse>(tokenJson);

                    if (tokenRes.StatusCode == HttpStatusCode.OK)
                        tokenResponse = tempTokenResponse;

                    if (tempTokenResponse.TokenType is "Bearer") {
                        AccessToken = tempTokenResponse.AccessToken;
                        RefreshToken = tempTokenResponse.RefreshToken;
                        return tokenResponse;
                    }

                    await Task.Delay(pollingInterval);
                    timeRemaining = codeExpiresOn - DateTimeOffset.UtcNow;
                }
                throw new("登录操作已超时");
            }
        }

        public new async ValueTask<MicrosoftAccount> AuthAsync(Action<string> func) {
            #region Other
            IProgress<string> progress = new Progress<string>();
            ((Progress<string>)progress).ProgressChanged += ProgressChanged!;

            void ProgressChanged(object _, string e) =>
                func(e);

            void Report(string value) {
                if (func != null) {
                    progress.Report(value);
                }
            }

            #endregion

            #region Authenticate with Refresh

            if (AuthType is AuthType.Refresh) {
                progress.Report("开始获取 AccessToken");
                var url = "https://login.live.com/oauth20_token.srf";
                string authContent = $"client_id={ClientId}" + $"&refresh_token={RefreshToken}" + "&grant_type=refresh_token";
                var result = await HttpWrapper.HttpPostAsync(url, authContent, "application/x-www-form-urlencoded");
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(await result.Content.ReadAsStringAsync());
                AccessToken = (tokenResponse?.AccessToken) ?? "None";
            }

            #endregion

            #region Authenticate with XBL

            progress.Report("开始获取 XBL 令牌");

            var xBLReqModel = new XBLAuthenticateRequestModel();
            xBLReqModel.Properties.RpsTicket = xBLReqModel.Properties.RpsTicket.Replace("<access token>", AccessToken);
            using var xBLReqModelPostRes = await HttpWrapper.HttpPostAsync($"https://user.auth.xboxlive.com/user/authenticate", JsonConvert.SerializeObject(xBLReqModel));
            var xBLResModel = JsonConvert.DeserializeObject<XBLAuthenticateResponseModel>(await xBLReqModelPostRes.Content.ReadAsStringAsync());

            #endregion

            #region Authenticate with XSTS

            Report("开始获取 XSTS令牌");

            var xSTSReqModel = new XSTSAuthenticateRequestModel();
            xSTSReqModel.Properties.UserTokens.Add(xBLResModel!.Token);

            using var xSTSReqModelPostRes = await HttpWrapper.HttpPostAsync($"https://xsts.auth.xboxlive.com/xsts/authorize", JsonConvert.SerializeObject(xSTSReqModel));
            var xSTSResModel = JsonConvert.DeserializeObject<XSTSAuthenticateResponseModel>(await xSTSReqModelPostRes.Content.ReadAsStringAsync());

            #endregion

            #region Authenticate with Minecraft

            Report("开始获取 Minecraft账户基础信息");
            string authenticateMinecraftPost =
                $"{{\"identityToken\":\"XBL3.0 x={xBLResModel.DisplayClaims.Xui[0]["uhs"]};{xSTSResModel!.Token}\"}}";

            using var authenticateMinecraftPostRes = await HttpWrapper.HttpPostAsync($"https://api.minecraftservices.com/authentication/login_with_xbox", authenticateMinecraftPost);
            string access_token = (string)JObject.Parse(await authenticateMinecraftPostRes.Content.ReadAsStringAsync())["access_token"]!;

            #endregion

            #region Check with Game
            Report("开始检查游戏所有权");
            var authorization = new Tuple<string, string>("Bearer", access_token!);

            bool hasGame = false;
            try {
                using var gameHasRes = await HttpWrapper.HttpGetAsync("https://api.minecraftservices.com/entitlements/mcstore", authorization);
                string json = await gameHasRes.Content.ReadAsStringAsync();
                var itemArray = json.ToJsonEntity<GameHasCheckResponseModel>();

                if (itemArray != null) {
                    hasGame = itemArray.Items.Count > 0 ? true : false;
                } else {
                    hasGame = true;
                }
            }
            catch (Exception ex) {
                Trace.WriteLine($"[错误] 未能成功检查游戏所有权，但会继续尝试获取 个人档案，错误的原因如下：{ex}");
            }
            #endregion

            #region Get the profile

            if (hasGame) {
                Report("开始获取 玩家Profile");
                using var profileRes = await HttpWrapper.HttpGetAsync("https://api.minecraftservices.com/minecraft/profile", authorization);
                var microsoftAuthenticationResponse = JsonConvert.DeserializeObject<MicrosoftAuthenticationResponse>(await profileRes.Content.ReadAsStringAsync());
                Report("微软登录（非刷新验证）完成");

                return new MicrosoftAccount {
                    AccessToken = access_token,
                    Type = AccountType.Microsoft,
                    ClientToken = Guid.NewGuid().ToString("N"),
                    Name = microsoftAuthenticationResponse!.Name,
                    Uuid = Guid.Parse(microsoftAuthenticationResponse.Id),
                    RefreshToken = string.IsNullOrEmpty(RefreshToken) ? "None" : RefreshToken,
                    DateTime = DateTime.Now
                };
            } else {
                throw new("未购买 Minecraft！");
            }

            throw new("验证失败！");
            #endregion
        }

        public override MicrosoftAccount Auth() => AuthAsync(null).GetAwaiter().GetResult();
    }

    partial class MicrosoftAuthenticator {
        public MicrosoftAuthenticator() { }

        public MicrosoftAuthenticator(AuthType authType = AuthType.Access) {
            AuthType = authType;
        }
    }

    partial class MicrosoftAuthenticator {
        public string ClientId { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;

        public AuthType AuthType { get; set; } = AuthType.Access;

        public string[] Scopes => new string[] { "XboxLive.signin", "offline_access", "openid", "profile", "email" };

        protected string AccessToken { get; set; } = string.Empty;
    }
}
