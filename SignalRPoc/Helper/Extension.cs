using Microsoft.AspNetCore.Http;
using SignalRPoc.Models;
using System;
using System.Linq;
using System.Text.Json;

namespace SignalRPoc.Helper
{
    public static class Extension
    {
        private static readonly string userKey = "UserSession";

        public static T FromJson<T>(this string json)
        {
            try
            {
                var result = JsonSerializer.Deserialize<T>(json);
                return result;
            }
            catch (Exception x)
            {
                return default;
            }
        }

        public static string ToJson(this object obj, bool ident = false)
        {
            try
            {
                var result = JsonSerializer.Serialize(obj, new JsonSerializerOptions()
                {
                    WriteIndented = ident
                });
                return result;
            }
            catch (Exception x)
            {
                return default;
            }
        }

        public static UserSession GetUserSession(this HttpContext ctx)
        {
            try
            {
                if (ctx.Session.Keys.Contains(userKey))
                {

                    var json = ctx.Session.GetString(userKey);
                    var result = json.FromJson<UserSession>();
                    return result;
                }
            }
            catch (Exception x)
            {
            }
            return default;
        }

        public static void SetUserSession(this HttpContext ctx, UserSession user)
        {
            try
            {
                var json = user.ToJson();
                ctx.Session.SetString(userKey, json);
            }
            catch (Exception x)
            {
            }
        }
    }
}
