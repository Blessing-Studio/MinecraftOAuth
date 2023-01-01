using MinecraftLaunch.Modules.Models.Auth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecaftOAuth.Module.Base
{
    /// <summary>
    /// 验证器抽象基类
    /// </summary>
    public abstract class AuthenticatorBase<T>
    {
        /// <summary>
        /// 验证方法
        /// </summary>
        /// <returns>游戏角色信息</returns>
        public abstract ValueTask<T> AuthAsync(Action<string> func);
        /// <summary>
        /// 验证方法
        /// </summary>
        /// <returns>游戏角色信息列表</returns>
        public abstract T Auth();
    }
}
